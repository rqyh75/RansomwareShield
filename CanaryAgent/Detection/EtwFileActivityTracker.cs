using CanaryAgent.Communication;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CanaryAgent.Detection
{
    public sealed class EtwFileActivityTracker : IDisposable
    {
        // Recent records keyed by normalised file path. Capped at 10 per path.
        // Pruned periodically to avoid unbounded growth in long-running sessions.
        private readonly ConcurrentDictionary<string, List<FileActivityRecord>> _recent =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string _sessionName;
        private Thread? _thread;
        private TraceEventSession? _session;
        private volatile bool _running;

        private NamedPipeSender? _pipeSender;

        private readonly ConcurrentDictionary<string, DateTime> _suppressedPaths =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, byte> _trackedCanaryFiles =
            new(StringComparer.OrdinalIgnoreCase);

        // Tracks when we last pruned the _recent cache.
        private DateTime _lastCachePrune = DateTime.UtcNow;
        private static readonly TimeSpan CachePruneInterval = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan CacheMaxAge         = TimeSpan.FromMinutes(10);

        public static EtwFileActivityTracker Instance { get; } = new();

        private EtwFileActivityTracker()
        {
            _sessionName = "CanaryAgent-FileIO-";
            _pipeSender  = new NamedPipeSender("CanaryAgentPipe");
        }

        public void Start()
        {
            if (_running) return;

            Task.Run(async () =>
            {
                if (_pipeSender != null)
                {
                    bool connected = await _pipeSender.ConnectAsync(10_000);
                    if (!connected)
                        Console.WriteLine("[Canary] Response Agent not running — will retry when alerts fire");
                }
            });

            _running = true;
            _thread  = new Thread(RunEtwLoop)
            {
                IsBackground = true,
                Name         = "CanaryAgent ETW File Tracker"
            };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _session?.Dispose(); } catch { }
        }

        public void Dispose() => Stop();

        public FileActivityRecord? TryGetRecent(string path, TimeSpan maxAge)
        {
            path = Normalize(path);
            if (_recent.TryGetValue(path, out var list))
            {
                lock (list)
                {
                    return list
                        .Where(r => DateTimeOffset.UtcNow - r.TimestampUtc <= maxAge)
                        .OrderByDescending(r => r.TimestampUtc)
                        .FirstOrDefault();
                }
            }
            return null;
        }

        public void SuppressPath(string path, TimeSpan duration)
        {
            _suppressedPaths[Normalize(path)] = DateTime.UtcNow.Add(duration);
        }

        // Exposed so CanaryWatcher can check suppression before raising alerts.
        public bool IsSuppressedPublic(string path) => IsSuppressed(path);

        private bool IsSuppressed(string path)
        {
            path = Normalize(path);
            if (_suppressedPaths.TryGetValue(path, out var until))
            {
                if (DateTime.UtcNow <= until) return true;
                _suppressedPaths.TryRemove(path, out _);
            }
            return false;
        }

        public void RefreshTrackedFiles(IEnumerable<string> files)
        {
            _trackedCanaryFiles.Clear();
            foreach (var f in files)
                _trackedCanaryFiles[Normalize(f)] = 1;
        }

        private bool IsCanaryFile(string path) =>
            _trackedCanaryFiles.ContainsKey(Normalize(path));

        private void RunEtwLoop()
        {
            try
            {
                using var session = new TraceEventSession(_sessionName);
                _session = session;
                session.StopOnDispose = true;

                session.EnableKernelProvider(
                    KernelTraceEventParser.Keywords.FileIOInit |
                    KernelTraceEventParser.Keywords.FileIO);

                session.Source.Kernel.FileIOWrite  += data => Record("modify", data.FileName, data.ProcessID);
                session.Source.Kernel.FileIODelete += data => Record("delete", data.FileName, data.ProcessID);
                session.Source.Kernel.FileIORename += data => Record("rename", data.FileName, data.ProcessID);

                session.Source.Process();
            }
            catch { }
            finally
            {
                _running = false;
            }
        }

        private void Record(string activityType, string? path, int processId)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var normalizedPath = Normalize(path);

            int    parentPid  = 0;
            string parentName = "unknown";
            string username   = "unknown";

            try
            {
                parentPid  = GetParentProcessId(processId);
                parentName = GetProcessNameSafe(parentPid);
                username   = GetProcessUsername(processId);
            }
            catch { }

            var record = new FileActivityRecord
            {
                ActivityType     = activityType,
                Path             = normalizedPath,
                ProcessId        = processId,
                ProcessName      = SafeGetProcessName(processId),
                ParentProcessId  = parentPid,
                ParentProcessName = parentName,
                Username         = username,
                TimestampUtc     = DateTimeOffset.UtcNow
            };

            bool isCanaryFile = IsCanaryFile(normalizedPath);

            if (isCanaryFile && IsSuppressed(normalizedPath))
            {
                Console.WriteLine($"[Canary] Suppressed self-generated {activityType} on {Path.GetFileName(normalizedPath)}");
                return;
            }

            if (isCanaryFile)
            {
                var alert = new CanaryAlert
                {
                    Timestamp         = record.TimestampUtc.UtcDateTime,
                    CanaryFile        = normalizedPath,
                    Action            = activityType,
                    ProcessId         = record.ProcessId,
                    ProcessName       = record.ProcessName,
                    ParentProcessId   = record.ParentProcessId,
                    ParentProcessName = record.ParentProcessName,
                    Username          = record.Username
                };

                _ = Task.Run(async () =>
                {
                    if (_pipeSender != null)
                        await _pipeSender.SendAlertAsync(alert);
                });

                Console.WriteLine($"\n🚨 CANARY ALERT! {activityType.ToUpper()} on {Path.GetFileName(normalizedPath)}");
                Console.WriteLine($"   Process: {record.ProcessName} (PID: {record.ProcessId})");
                Console.WriteLine($"   Parent:  {record.ParentProcessName} (PID: {record.ParentProcessId})");
                Console.WriteLine($"   User:    {record.Username}");
            }

            // Store in recent cache
            _recent.AddOrUpdate(
                normalizedPath,
                _ => new List<FileActivityRecord> { record },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(record);
                        if (list.Count > 10)
                            list.RemoveAt(0);
                    }
                    return list;
                });

            // Periodically prune old entries from the cache to prevent unbounded growth.
            MaybePruneCache();
        }

        private void MaybePruneCache()
        {
            if (DateTime.UtcNow - _lastCachePrune < CachePruneInterval) return;
            _lastCachePrune = DateTime.UtcNow;

            var cutoff  = DateTimeOffset.UtcNow - CacheMaxAge;
            var toRemove = new List<string>();

            foreach (var kvp in _recent)
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(r => r.TimestampUtc < cutoff);
                    if (kvp.Value.Count == 0)
                        toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
                _recent.TryRemove(key, out _);
        }

        private static string Normalize(string path)
        {
            try { return Path.GetFullPath(path.Trim()).ToLowerInvariant(); }
            catch { return path.Trim().ToLowerInvariant(); }
        }

        private static string SafeGetProcessName(int processId)
        {
            try { return Process.GetProcessById(processId).ProcessName; }
            catch { return "unknown"; }
        }

        private static int GetParentProcessId(int processId)
        {
            try
            {
                var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}";
                using var searcher = new System.Management.ManagementObjectSearcher(query);
                foreach (System.Management.ManagementObject obj in searcher.Get())
                    return Convert.ToInt32(obj["ParentProcessId"]);
            }
            catch { }
            return 0;
        }

        private static string GetProcessNameSafe(int processId)
        {
            if (processId == 0) return "system";
            try { return Process.GetProcessById(processId).ProcessName; }
            catch { return "unknown"; }
        }

        // Returns the actual username of the process owner using Win32 token APIs.
        // The previous implementation incorrectly returned Environment.UserName (the agent's own user).
        private static string GetProcessUsername(int processId)
        {
            IntPtr processHandle = IntPtr.Zero;
            IntPtr tokenHandle   = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(0x0400 /* PROCESS_QUERY_INFORMATION */, false, processId);
                if (processHandle == IntPtr.Zero) return "unknown";

                if (!OpenProcessToken(processHandle, 8 /* TOKEN_QUERY */, out tokenHandle))
                    return "unknown";

                if (!GetTokenInformation(tokenHandle, 1 /* TokenUser */,
                        IntPtr.Zero, 0, out uint tokenInfoLength) &&
                    tokenInfoLength == 0)
                    return "unknown";

                IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);
                try
                {
                    if (!GetTokenInformation(tokenHandle, 1, tokenInfo, tokenInfoLength, out _))
                        return "unknown";

                    IntPtr sid = Marshal.ReadIntPtr(tokenInfo);
                    if (!ConvertSidToStringSid(sid, out string? sidStr) || sidStr == null)
                        return "unknown";

                    // Translate SID to account name
                    int nameLen   = 256;
                    int domainLen = 256;
                    var name      = new System.Text.StringBuilder(nameLen);
                    var domain    = new System.Text.StringBuilder(domainLen);

                    if (LookupAccountSid(null, sid, name, ref nameLen, domain, ref domainLen, out _))
                        return domain.Length > 0 ? $"{domain}\\{name}" : name.ToString();

                    return sidStr;
                }
                finally
                {
                    Marshal.FreeHGlobal(tokenInfo);
                }
            }
            catch
            {
                return "unknown";
            }
            finally
            {
                if (tokenHandle   != IntPtr.Zero) CloseHandle(tokenHandle);
                if (processHandle != IntPtr.Zero) CloseHandle(processHandle);
            }
        }

        public async Task SendTestAlert(CanaryAlert alert)
        {
            if (_pipeSender != null)
                await _pipeSender.SendAlertAsync(alert);
        }

        #region Native Win32 for username resolution

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inheritHandle, int processId);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess,
            out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr tokenHandle, int tokenInfoClass,
            IntPtr tokenInfo, uint tokenInfoLength, out uint returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool ConvertSidToStringSid(IntPtr sid, out string? stringSid);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupAccountSid(string? systemName, IntPtr sid,
            System.Text.StringBuilder name, ref int cbName,
            System.Text.StringBuilder referencedDomainName, ref int cbReferencedDomainName,
            out int peUse);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        #endregion
    }

    public sealed class FileActivityRecord
    {
        public string          ActivityType      { get; set; } = "";
        public string          Path              { get; set; } = "";
        public int             ProcessId         { get; set; }
        public string          ProcessName       { get; set; } = "unknown";
        public int             ParentProcessId   { get; set; }
        public string          ParentProcessName { get; set; } = "unknown";
        public string          Username          { get; set; } = "unknown";
        public DateTimeOffset  TimestampUtc      { get; set; }
    }
}
