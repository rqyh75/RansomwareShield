using antiRansomware.ProcessMonitor;
using antiRansomware.ResponseAgent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace antiRansomware
{
    class Program
    {
        // ── paths ──────────────────────────────────────────────────────────────
        // Adjust these three constants to match your machine if they ever move.
        private const string MinifilterDriverName = "Minifltterdriver";
        private static readonly string CanaryProject = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"CanaryAgent\CanaryAgent\CanaryAgent.csproj");
        private static readonly string DashboardRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dashboard");
        private const int DashboardBackendPort = 5000;
        private const int DashboardFrontendPort = 5173;

        // ── state ──────────────────────────────────────────────────────────────
        private static readonly HashSet<int> killedParents = new();
        private static readonly Dictionary<int, ProcessInfo> recentProcesses = new();
        private static readonly object processCacheLock = new();
        private const int KilledParentsMaxSize = 500;

        private static readonly List<Process> childProcesses = new();
        private static readonly object childLock = new();

        // ══════════════════════════════════════════════════════════════════════
        //  ENTRY POINT
        // ══════════════════════════════════════════════════════════════════════
        static void Main(string[] args)
        {
            ConsoleHub.Banner();

            // ── admin check ──────────────────────────────────────────────────
            if (!IsAdministrator())
            {
                ConsoleHub.Error("This application must be run as Administrator.");
                ConsoleHub.Info("Right-click the executable / Visual Studio and choose: Run as Administrator.");
                ConsoleHub.Info("Press any key to exit...");
                Console.ReadKey();
                return;
            }
            ConsoleHub.Success("Running with Administrator privileges.");

            // ── Ctrl+C / process exit: kill all children ─────────────────────
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; Shutdown(); };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();

            // ══ STEP 1 — load minifilter driver ═════════════════════════════
            LoadMinifilterDriver(MinifilterDriverName);

            // ══ STEP 2 — start dashboard backend (Spring Boot) ══════════════
            StartChildProcess(
                label: "DASH-API",
                color: ConsoleColor.DarkCyan,
                fileName: "cmd.exe",
                arguments: "/c mvn spring-boot:run",
                workDir: Path.Combine(DashboardRoot, "backend-java"),
                mode: OutputMode.ReadyLine,
                readyToken: "Started ",
                readySummary: $"Dashboard API ready → http://localhost:{DashboardBackendPort}/api/events");

            // Give the backend a moment to bind the port before the UI tries to connect.
            ConsoleHub.System("Waiting 6 s for Dashboard API to initialise...");
            Thread.Sleep(6000);
            ConsoleHub.Info($"Dashboard API → http://localhost:{DashboardBackendPort}");

            // ══ STEP 3 — start dashboard frontend (React / npm) ══════════════
            StartChildProcess(
                label: "DASH-UI",
                color: ConsoleColor.Cyan,
                fileName: "cmd.exe",
                arguments: "/c npm run dev",
                workDir: Path.Combine(DashboardRoot, "sim-dashboard"),
                mode: OutputMode.ReadyLine,
                readyToken: "Compiled",          // React prints "Compiled successfully!"
                readySummary: $"Dashboard UI  ready → http://localhost:{DashboardFrontendPort}");

            Thread.Sleep(1500);

            // ══ STEP 4 — start Canary Agent ══════════════════════════════════
            StartChildProcess(
                label: "CANARY",
                color: ConsoleColor.Magenta,
                fileName: "dotnet.exe",
                arguments: $@"run --project ""{CanaryProject}""",
                workDir: null,
                mode: OutputMode.Full);   // show all canary output in this terminal

            Thread.Sleep(1200);

            // ══ STEP 5 — wire up Response Agent subsystems ════════════════════

            // ── rules ────────────────────────────────────────────────────────
            ConsoleHub.System("Loading detection rules...");
            string rulesPath = Path.Combine(Directory.GetCurrentDirectory(), "rules.json");
            var ruleEngine = new RuleEngine();
            ruleEngine.LoadRules(rulesPath);
            ConsoleHub.Success("Detection rules loaded.");

            // ── canary pipe listener ─────────────────────────────────────────
            ConsoleHub.System("Starting Canary Agent pipe listener...");
            using var canaryPipeServer = new CanaryPipeServer(OnCanaryAlertReceived);
            canaryPipeServer.Start();
            ConsoleHub.Success("Canary pipe listener started.");

            // ── minifilter listener ──────────────────────────────────────────
            ConsoleHub.System("Starting Minifilter listener...");
            using var minifilterClient = new MinifilterMessageClient();
            minifilterClient.OnMessageReceived += OnMinifilterMessageReceived;
            try
            {
                minifilterClient.Start();
                ConsoleHub.Success("Minifilter listener started.");
            }
            catch (Exception ex)
            {
                ConsoleHub.Warning($"Minifilter listener failed to start: {ex.Message}");
                ConsoleHub.Warning("The driver may not be loaded or the communication port was not created.");
            }

            // ── ETW process monitor ──────────────────────────────────────────
            using var monitor = new ETWProcessMonitor();
            monitor.OnProcessDetected += processInfo =>
            {
                lock (processCacheLock)
                {
                    recentProcesses[processInfo.ProcessId] = processInfo;
                    if (recentProcesses.Count > 1000)
                        foreach (var key in recentProcesses.Keys.Take(100).ToList())
                            recentProcesses.Remove(key);
                }

                var matches = ruleEngine.MatchProcess(processInfo);
                if (matches.Count == 0) return;

                var alert = new Alert
                {
                    Timestamp = DateTime.UtcNow,
                    Source = "process_monitor",
                    MatchedRules = matches,
                    Severity = matches[0].Rule.Severity,
                    Process = processInfo,
                    ResponseTaken = matches[0].Rule.Response
                };

                ConsoleHub.Section($"ETW ALERT [{alert.Severity.ToUpper()}]", GetSeverityColor(alert.Severity));
                ConsoleHub.Etw($"Process : {processInfo.ProcessName} PID {processInfo.ProcessId}");
                ConsoleHub.Etw($"Parent  : {processInfo.ParentProcessName} PID {processInfo.ParentProcessId}");
                ConsoleHub.Etw($"Command : {processInfo.CommandLine}");
                foreach (var m in matches)
                    ConsoleHub.Etw($"Rule    : [{m.Rule.RuleId}] {m.Rule.Name} - {m.Reason}");
                ConsoleHub.Etw($"Response: {alert.ResponseTaken}");

                alert.LogToFile();
                _ = DashboardIntegration.SendAsync(DashboardIntegration.FromEtwAlert(alert));

                if (matches[0].Rule.Response != "terminate_process") return;

                int parentPid = processInfo.ParentProcessId;
                if (parentPid <= 0) return;

                lock (processCacheLock)
                {
                    if (killedParents.Contains(parentPid)) return;
                    if (killedParents.Count >= KilledParentsMaxSize)
                        killedParents.Remove(killedParents.First());
                    killedParents.Add(parentPid);
                }

                string parentName = GetProcessNameSafe(parentPid);
                ConsoleHub.Critical($"PARENT PROCESS KILLED: {parentName} PID {parentPid}");
                ConsoleHub.Critical($"Reason: Malicious child detected: {processInfo.ProcessName} PID {processInfo.ProcessId}");
                AppendDetectionLog(
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | PARENT_KILLED | {parentName} | PID:{parentPid} | Reason:{processInfo.ProcessName} triggered {matches[0].Rule.RuleId}");
                KillProcessTree(parentPid);
            };

            ConsoleHub.System("Starting ETW Process Monitor...");
            monitor.Start();
            ConsoleHub.Success("ETW Process Monitor started.");

            // ══ Ready ═════════════════════════════════════════════════════════
            ConsoleHub.Section("RANSOMSHIELD IS RUNNING", ConsoleColor.Green);
            ConsoleHub.Info($"Dashboard UI  → http://localhost:{DashboardFrontendPort}");
            ConsoleHub.Info($"Dashboard API → http://localhost:{DashboardBackendPort}");
            ConsoleHub.Info("All components active. Press ENTER to stop everything.");

            Console.ReadLine();
            Shutdown();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MINIFILTER DRIVER LOADER
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Runs  fltmc load &lt;driverName&gt;  as an elevated subprocess.
        /// Gracefully handles the case where the driver is already loaded.
        /// </summary>
        private static void LoadMinifilterDriver(string driverName)
        {
            ConsoleHub.System($"Loading minifilter driver: {driverName}...");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "fltmc.exe",
                    Arguments = $"load {driverName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi)!;
                string stdout = proc.StandardOutput.ReadToEnd().Trim();
                string stderr = proc.StandardError.ReadToEnd().Trim();
                proc.WaitForExit();

                if (!string.IsNullOrWhiteSpace(stdout))
                    ConsoleHub.Minifilter($"fltmc: {stdout}");

                // Exit code 0 = success; 0x80070422 / other = already loaded or error
                if (proc.ExitCode == 0)
                {
                    ConsoleHub.Success($"Minifilter driver '{driverName}' loaded successfully.");
                }
                else if (stderr.Contains("1056", StringComparison.OrdinalIgnoreCase) ||
                         stderr.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                         stdout.Contains("already", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleHub.Info($"Minifilter driver '{driverName}' is already loaded.");
                }
                else
                {
                    // Non-fatal: log the error and keep going.  The listener will report
                    // its own failure if the port really is unavailable.
                    string detail = !string.IsNullOrWhiteSpace(stderr) ? stderr : $"exit code {proc.ExitCode}";
                    ConsoleHub.Warning($"fltmc load returned: {detail}");
                    ConsoleHub.Warning("Continuing — the driver may already be loaded or registered differently.");
                }
            }
            catch (Exception ex)
            {
                ConsoleHub.Warning($"Could not run fltmc.exe: {ex.Message}");
                ConsoleHub.Warning("Make sure fltmc.exe is on PATH (it is in System32 on any Windows install).");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CHILD PROCESS LAUNCHER  (Canary Agent / Dashboard)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Controls how a child process's stdout/stderr is surfaced in the console.
        /// Full      — every line is printed (e.g. Canary Agent).
        /// Silent    — output is discarded; only the one-line start/exit status shows.
        /// ReadyLine — output is suppressed UNTIL a line containing <c>readyToken</c>
        ///             appears, at which point exactly one clean summary line is printed
        ///             and all further output is discarded.  Ideal for Spring Boot /
        ///             long build processes.
        /// </summary>
        private enum OutputMode { Full, Silent, ReadyLine }

        private static void StartChildProcess(
            string label,
            ConsoleColor color,
            string fileName,
            string arguments,
            string? workDir,
            OutputMode mode = OutputMode.Full,
            string readyToken = "",      // used only with ReadyLine
            string readySummary = "")    // one-liner to print when ready
        {
            ConsoleHub.System($"Starting {label}...");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workDir ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

                // Track whether the ready-line has already been printed (thread-safe).
                int readyFired = 0;

                proc.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;

                    switch (mode)
                    {
                        case OutputMode.Full:
                            ConsoleHub.Child(label, e.Data, color);
                            break;

                        case OutputMode.Silent:
                            // Discard — show nothing
                            break;

                        case OutputMode.ReadyLine:
                            // Watch for the token; print the summary exactly once, then go silent.
                            if (Interlocked.CompareExchange(ref readyFired, 1, 0) == 0)
                            {
                                if (e.Data.Contains(readyToken, StringComparison.OrdinalIgnoreCase))
                                {
                                    string summary = string.IsNullOrWhiteSpace(readySummary)
                                        ? $"{label} is ready."
                                        : readySummary;
                                    ConsoleHub.Success(summary);
                                }
                                else
                                {
                                    // Not the ready line yet — put the counter back so we keep watching.
                                    Interlocked.Exchange(ref readyFired, 0);
                                }
                            }
                            // Once fired (readyFired == 1) all subsequent lines are silently discarded.
                            break;
                    }
                };

                proc.ErrorDataReceived += (_, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;

                    // For ReadyLine / Silent processes, only surface lines that look like
                    // genuine errors (contain "ERROR" or "Exception"), not build warnings.
                    if (mode == OutputMode.Full)
                    {
                        ConsoleHub.Child(label, e.Data, ConsoleColor.DarkYellow);
                    }
                    else if (e.Data.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                             e.Data.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
                             e.Data.Contains("FAILED", StringComparison.OrdinalIgnoreCase))
                    {
                        ConsoleHub.Child(label, e.Data, ConsoleColor.Red);
                    }
                };

                proc.Exited += (_, _) =>
                {
                    ConsoleHub.Warning($"{label} process exited (code {proc.ExitCode}).");
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                lock (childLock) childProcesses.Add(proc);

                ConsoleHub.Success($"{label} started (PID {proc.Id}).");
            }
            catch (Exception ex)
            {
                ConsoleHub.Error($"Failed to start {label}: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SHUTDOWN
        // ══════════════════════════════════════════════════════════════════════

        private static bool _shuttingDown = false;

        private static void Shutdown()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;

            ConsoleHub.Section("SHUTTING DOWN", ConsoleColor.Yellow);

            // Kill all child processes (canary agent, dashboard backend/frontend)
            lock (childLock)
            {
                foreach (var proc in childProcesses)
                {
                    try
                    {
                        if (!proc.HasExited)
                        {
                            ConsoleHub.Warning($"Stopping child process PID {proc.Id}...");
                            proc.Kill(entireProcessTree: true);
                        }
                    }
                    catch { /* process may already be gone */ }
                }
            }

            ConsoleHub.Success("All components stopped. Goodbye.");
            Environment.Exit(0);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CANARY ALERT HANDLER
        // ══════════════════════════════════════════════════════════════════════

        private static bool IsTrustedCanaryProcess(CanaryAlert alert)
        {
            string proc = (alert.ProcessName ?? "").ToLowerInvariant();
            string parent = (alert.ParentProcessName ?? "").ToLowerInvariant();
            return proc == "canaryagent" || proc == "canaryagent.exe" ||
                   parent == "canaryagent" || parent == "canaryagent.exe" ||
                   proc == "dotnet" || parent == "dotnet";
        }

        private static Task OnCanaryAlertReceived(CanaryAlert alert)
        {
            if (IsTrustedCanaryProcess(alert))
            {
                ConsoleHub.Info("Ignoring canary agent self-generated activity.");
                return Task.CompletedTask;
            }

            ConsoleHub.Section("CANARY ALERT RECEIVED", ConsoleColor.Magenta);
            ConsoleHub.Canary($"Time    : {alert.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            ConsoleHub.Canary($"File    : {alert.CanaryFile}");
            ConsoleHub.Canary($"Action  : {alert.Action}");
            ConsoleHub.Canary($"Process : {alert.ProcessName} PID {alert.ProcessId}");
            ConsoleHub.Canary($"Parent  : {alert.ParentProcessName} PID {alert.ParentProcessId}");
            ConsoleHub.Canary($"User    : {alert.Username}");

            AppendDetectionLog(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | CANARY_ALERT | File:{alert.CanaryFile} | Action:{alert.Action} | Process:{alert.ProcessName}({alert.ProcessId}) | Parent:{alert.ParentProcessName}({alert.ParentProcessId})");

            lock (processCacheLock)
            {
                if (recentProcesses.ContainsKey(alert.ProcessId))
                    ConsoleHub.Success($"Correlation: {alert.ProcessName} PID {alert.ProcessId} was also seen by ETW.");
            }

            if (alert.ParentProcessId <= 0) goto SendDashboard;

            bool alreadyKilled;
            lock (processCacheLock)
            {
                alreadyKilled = killedParents.Contains(alert.ParentProcessId);
                if (!alreadyKilled)
                {
                    if (killedParents.Count >= KilledParentsMaxSize)
                        killedParents.Remove(killedParents.First());
                    killedParents.Add(alert.ParentProcessId);
                }
            }

            if (alreadyKilled)
            {
                ConsoleHub.Info($"Parent PID {alert.ParentProcessId} already killed. Skipping duplicate.");
                goto SendDashboard;
            }

            ConsoleHub.Critical("CANARY TRIGGER: Killing parent process tree.");
            ConsoleHub.Critical($"Parent PID: {alert.ParentProcessId} ({alert.ParentProcessName})");
            ConsoleHub.Critical($"Reason: Canary file {alert.Action} by {alert.ProcessName}");

            KillProcessTree(alert.ParentProcessId);

            AppendDetectionLog(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | CANARY_KILL | Parent:{alert.ParentProcessName}({alert.ParentProcessId}) | Child:{alert.ProcessName}({alert.ProcessId}) | Action:{alert.Action}");

        SendDashboard:
            _ = DashboardIntegration.SendAsync(DashboardIntegration.FromCanaryAlert(alert));
            return Task.CompletedTask;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MINIFILTER MESSAGE HANDLER
        // ══════════════════════════════════════════════════════════════════════

        private static void OnMinifilterMessageReceived(MinifilterNotification msg)
        {
            ConsoleHub.Section("MINIFILTER EVENT RECEIVED", ConsoleColor.Blue);
            ConsoleHub.Minifilter($"PID      : {msg.ProcessId}");
            ConsoleHub.Minifilter($"Process  : {msg.ProcessName}");
            ConsoleHub.Minifilter($"Action   : {msg.Action}");
            ConsoleHub.Minifilter($"Response : {msg.Response}");
            ConsoleHub.Minifilter($"Target   : {msg.TargetPath}");

            AppendDetectionLog(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | MINIFILTER | PID:{msg.ProcessId} | Process:{msg.ProcessName} | Action:{msg.Action} | Response:{msg.Response} | Target:{msg.TargetPath}");

            _ = DashboardIntegration.SendAsync(DashboardIntegration.FromMinifilterNotification(msg));

            // ── Kill the offending process when the driver says "block" ──────
            // The driver already denied the file operation at the kernel level,
            // but the process is still alive and will keep retrying. We must
            // terminate it from user-mode so it cannot continue.
            if (!msg.Response.Equals("block", StringComparison.OrdinalIgnoreCase))
                return;

            int pid = (int)msg.ProcessId;
            if (pid <= 0) return;

            // De-duplicate: don't kill the same PID more than once.
            bool alreadyKilled;
            lock (processCacheLock)
            {
                alreadyKilled = killedParents.Contains(pid);
                if (!alreadyKilled)
                {
                    if (killedParents.Count >= KilledParentsMaxSize)
                        killedParents.Remove(killedParents.First());
                    killedParents.Add(pid);
                }
            }

            if (alreadyKilled)
            {
                ConsoleHub.Info($"[Minifilter] PID {pid} ({msg.ProcessName}) already killed. Skipping duplicate.");
                return;
            }

            ConsoleHub.Critical($"[Minifilter] KILLING PROCESS: {msg.ProcessName} (PID {pid})");
            ConsoleHub.Critical($"[Minifilter] Reason: {msg.Action} → {msg.TargetPath}");

            AppendDetectionLog(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | MINIFILTER_KILL | {msg.ProcessName} | PID:{pid} | Action:{msg.Action} | Target:{msg.TargetPath}");

            KillProcessTree(pid);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private static void AppendDetectionLog(string line)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "detection_log.txt");
            File.AppendAllText(logPath, line + Environment.NewLine);
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static string GetProcessNameSafe(int pid)
        {
            if (pid == 0) return "system";
            try { return Process.GetProcessById(pid).ProcessName; }
            catch { return "unknown"; }
        }

        private static void KillProcessTree(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                string procName = process.ProcessName;
                ConsoleHub.Warning($"Attempting to kill process tree: {procName} PID {pid}");
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);

                if (process.HasExited)
                {
                    ConsoleHub.Success($"Successfully killed: {procName} PID {pid}");
                    AppendDetectionLog(
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | KILL_SUCCESS | {procName} | PID:{pid}");
                }
                else
                {
                    ConsoleHub.Warning($"Process PID {pid} did not exit after 5 seconds.");
                }
            }
            catch (ArgumentException) { ConsoleHub.Info($"Process PID {pid} already exited."); }
            catch (Exception ex) { ConsoleHub.Error($"Failed to terminate process: {ex.Message}"); }
        }

        private static ConsoleColor GetSeverityColor(string severity) =>
            severity.ToLowerInvariant() switch
            {
                "critical" => ConsoleColor.Red,
                "high" => ConsoleColor.Yellow,
                "medium" => ConsoleColor.Cyan,
                "low" => ConsoleColor.White,
                _ => ConsoleColor.Gray
            };
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CANARY PIPE SERVER  (unchanged logic, kept in same file)
    // ══════════════════════════════════════════════════════════════════════════

    public class CanaryPipeServer : IDisposable
    {
        private NamedPipeServerStream? _pipeServer;
        private bool _isRunning;
        private readonly Func<CanaryAlert, Task> _onAlertReceived;
        private const string PipeName = "CanaryAgentPipe";

        public CanaryPipeServer(Func<CanaryAlert, Task> onAlertReceived)
        {
            _onAlertReceived = onAlertReceived;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _ = Task.Run(ListenForCanaryAlerts);
        }

        private async Task ListenForCanaryAlerts()
        {
            while (_isRunning)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    ConsoleHub.Canary("Waiting for Canary Agent connection...");
                    await _pipeServer.WaitForConnectionAsync();
                    ConsoleHub.Success("Canary Agent connected via pipe.");

                    using var reader = new StreamReader(_pipeServer, Encoding.UTF8);
                    string? line;

                    while (_isRunning && (line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var alert = JsonSerializer.Deserialize<CanaryAlert>(line);
                            if (alert != null)
                                await _onAlertReceived(alert);
                        }
                        catch (JsonException ex)
                        {
                            ConsoleHub.Error($"Canary pipe JSON parse error: {ex.Message}");
                        }
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { ConsoleHub.Error($"Canary pipe error: {ex.Message}"); }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }

                if (_isRunning) await Task.Delay(1000);
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _pipeServer?.Dispose();
        }
    }
}
