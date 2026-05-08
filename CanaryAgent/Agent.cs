using CanaryAgent.Actors;
using CanaryAgent.Detection;
using CanaryAgent.Storage;
using CanaryAgent.DataGen;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace CanaryAgent.Core
{
    internal class Agent
    {
        private readonly Utils.TimeProvider time;
        private readonly StateStore state;
        private readonly FileActor fileActor;
        private readonly string statePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "CanaryAgent",
                         "canary_state.json");

        private readonly List<FileSystemWatcher> watchers = new();
        private readonly AutoResetEvent wakeSignal = new(false);
        private readonly ConcurrentQueue<string> detectionQueue = new();

        public Agent()
        {
            time = new Utils.TimeProvider();
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            state = StateStore.Load(statePath);

            // Remove stale tracked files that no longer exist on disk.
            // Without this, CanaryWatcher fires false-positive delete alerts on startup.
            int removed = state.Files.RemoveAll(f => !File.Exists(f.FullPath));
            if (removed > 0)
                Console.WriteLine($"[Agent] Pruned {removed} stale file(s) from state.");

            fileActor = new FileActor();

            Console.WriteLine("[Agent] Starting ETW File Activity Tracker...");
            EtwFileActivityTracker.Instance.Start();
            Console.WriteLine("[Agent] ETW Tracker started");
        }

        public void RunOneCycle()
        {
            var now = time.Now();
            var rng = new Random();

            EtwFileActivityTracker.Instance.RefreshTrackedFiles(
                state.Files.Select(f => f.FullPath));

            foreach (var f in state.Files)
            {
                if (f.NextModificationTime == DateTime.MinValue)
                    f.NextModificationTime = GetNextModificationTime(now, rng);

                if (f.NextRenameTime == DateTime.MinValue)
                    f.NextRenameTime = GetNextRenameTime(now, rng);
            }

            // Check for tampering BEFORE any actions
            CanaryWatcher.CheckFiles(state);

            state.LastRunDate = now;

            // Only perform file lifecycle actions during working hours (08:00–18:00).
            if (now.Hour < 8 || now.Hour > 18)
            {
                state.Save(statePath);
                return;
            }

            var selector = new ActionSelector();
            var action = selector.Decide(state, now);

            var candidates = TargetDirectories.GetWritableCandidateDirectories();
            Console.WriteLine($"Writable target dirs: {candidates.Count}");

            foreach (var f in state.Files)
                Console.WriteLine($"File: {f.FullPath}");

            Console.WriteLine($"Chosen action: {action}");

            switch (action)
            {
                case AgentAction.CreateFile:
                {
                    if (state.NextFileCreationTime != DateTime.MinValue &&
                        now < state.NextFileCreationTime)
                        break;

                    if (!LifecycleRules.CanCreateNewFile(state.Files.Count))
                        break;

                    var baseRoot = TargetDirectories.PickRandomDirectory(rng, candidates);
                    var allTypes = (PersonaType[])Enum.GetValues(typeof(PersonaType));
                    var topic    = allTypes[rng.Next(allTypes.Length)];
                    var topicDir = Path.Combine(baseRoot, FileActor.TopicFolderName(topic));
                    Directory.CreateDirectory(topicDir);

                    var created = fileActor.CreateFileFromPersonaType(topicDir, now, state.Files, topic);
                    if (created != null)
                    {
                        created.NextModificationTime = GetNextModificationTime(now, rng);
                        created.NextRenameTime       = GetNextRenameTime(now, rng);
                        state.Files.Add(created);
                        Console.WriteLine($"Created file: {created.FullPath}");
                        state.NextFileCreationTime = GetNextCreationTime(now, rng);
                    }
                    else
                    {
                        Console.WriteLine("No available persona found for selected topic.");
                    }
                    break;
                }

                case AgentAction.ModifyFile:
                {
                    var toModify = state.Files
                        .Where(f => f.NextModificationTime != DateTime.MinValue &&
                                    now >= f.NextModificationTime)
                        .OrderBy(f => f.NextModificationTime)
                        .FirstOrDefault();

                    if (toModify != null)
                    {
                        EtwFileActivityTracker.Instance.SuppressPath(toModify.FullPath, TimeSpan.FromSeconds(3));
                        fileActor.AppendRealisticContent(toModify, now);
                        toModify.NextModificationTime = GetNextModificationTime(now, rng);
                        Console.WriteLine($"Modified file: {toModify.FullPath}");
                    }
                    else
                    {
                        Console.WriteLine("No file is due for modification.");
                    }
                    break;
                }

                case AgentAction.RenameFile:
                {
                    var toRename = state.Files
                        .Where(f => f.NextRenameTime != DateTime.MinValue &&
                                    now >= f.NextRenameTime)
                        .OrderBy(f => f.NextRenameTime)
                        .FirstOrDefault();

                    if (toRename != null)
                    {
                        EtwFileActivityTracker.Instance.SuppressPath(toRename.FullPath, TimeSpan.FromSeconds(3));
                        var newVersion = fileActor.CopyFileWithVersion(toRename, now, state.Files);
                        toRename.NextRenameTime = GetNextRenameTime(now, rng);

                        if (newVersion != null)
                        {
                            EtwFileActivityTracker.Instance.SuppressPath(newVersion.FullPath, TimeSpan.FromSeconds(3));
                            newVersion.NextModificationTime = GetNextModificationTime(now, rng);
                            newVersion.NextRenameTime       = GetNextRenameTime(now, rng);
                            state.Files.Add(newVersion);
                            Console.WriteLine($"Created version: {newVersion.FullPath}");
                        }
                        else
                        {
                            Console.WriteLine("Rename/versioning was due, but no new version was created.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No file is due for rename.");
                    }
                    break;
                }

                case AgentAction.None:
                    break;
            }

            state.Save(statePath);
            Console.WriteLine($"\nState saved to: {statePath}");
        }

        public void RunContinuously()
        {
            RunOneCycle();
            SetupWatchers();

            while (true)
            {
                ProcessQueuedDetections();

                var nextDue = GetNextDueTime();

                if (nextDue == null)
                {
                    // No scheduled work; if currently outside work hours, sleep until 08:00
                    // to avoid a busy-loop that would fire RunOneCycle every few seconds all night.
                    var sleepUntil = GetNextWorkStart();
                    var wait = sleepUntil - DateTime.Now;
                    if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                    wakeSignal.WaitOne(wait);
                }
                else
                {
                    var wait = nextDue.Value - DateTime.Now;
                    if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                    wakeSignal.WaitOne(wait);
                }

                ProcessQueuedDetections();
                RunOneCycle();
                SetupWatchers();
            }
        }

        // Returns the next 08:00 start time (today if it hasn't passed, otherwise tomorrow).
        private static DateTime GetNextWorkStart()
        {
            var now   = DateTime.Now;
            var today = now.Date.AddHours(8);
            return now < today ? today : today.AddDays(1);
        }

        private static DateTime GetNextCreationTime(DateTime now, Random rng)
        {
            int minutes = rng.Next(30, 181);
            return now.AddMinutes(minutes);
        }

        private static DateTime GetNextModificationTime(DateTime now, Random rng)
        {
            int minutes = rng.Next(30, 241);
            return now.AddMinutes(minutes);
        }

        private static DateTime GetNextRenameTime(DateTime now, Random rng)
        {
            int days = rng.Next(1, 4);
            return now.AddDays(days);
        }

        private void SetupWatchers()
        {
            foreach (var w in watchers)
                w.Dispose();
            watchers.Clear();

            var directories = state.Files
                .Select(f => Path.GetDirectoryName(f.FullPath))
                .Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var dir in directories)
            {
                var watcher = new FileSystemWatcher(dir!)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName |
                                   NotifyFilters.LastWrite |
                                   NotifyFilters.Size
                };

                watcher.Changed += OnFileSystemEvent;
                watcher.Created += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnRenamedEvent;
                watcher.EnableRaisingEvents = true;

                watchers.Add(watcher);
            }
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            bool isTracked = state.Files.Any(f =>
                string.Equals(f.FullPath, e.FullPath, StringComparison.OrdinalIgnoreCase));

            if (isTracked)
            {
                detectionQueue.Enqueue(e.FullPath);
                wakeSignal.Set();
            }
        }

        private void OnRenamedEvent(object sender, RenamedEventArgs e)
        {
            bool oldTracked = state.Files.Any(f =>
                string.Equals(f.FullPath, e.OldFullPath, StringComparison.OrdinalIgnoreCase));
            bool newTracked = state.Files.Any(f =>
                string.Equals(f.FullPath, e.FullPath, StringComparison.OrdinalIgnoreCase));

            if (oldTracked || newTracked)
            {
                detectionQueue.Enqueue(e.OldFullPath);
                detectionQueue.Enqueue(e.FullPath);
                wakeSignal.Set();
            }
        }

        private void ProcessQueuedDetections()
        {
            bool hadDetection = false;
            while (detectionQueue.TryDequeue(out _))
                hadDetection = true;

            if (hadDetection)
            {
                CanaryWatcher.CheckFiles(state);
                state.Save(statePath);
            }
        }

        private DateTime? GetNextDueTime()
        {
            var dueTimes = new List<DateTime>();

            if (LifecycleRules.CanCreateNewFile(state.Files.Count) &&
                state.NextFileCreationTime != DateTime.MinValue)
                dueTimes.Add(state.NextFileCreationTime);

            foreach (var f in state.Files)
            {
                if (f.NextModificationTime != DateTime.MinValue)
                    dueTimes.Add(f.NextModificationTime);
                if (f.NextRenameTime != DateTime.MinValue)
                    dueTimes.Add(f.NextRenameTime);
            }

            if (dueTimes.Count == 0) return null;
            return dueTimes.Min();
        }
    }
}
