using CanaryAgent.Storage;
using System.Security.Cryptography;

namespace CanaryAgent.Detection
{
    public class CanaryWatcher
    {
        public static void CheckFiles(StateStore state)
        {
            foreach (var file in state.Files)
            {
                if (!File.Exists(file.FullPath))
                {
                    // Do not alert on paths the agent just suppressed (e.g. it renamed the file itself).
                    if (EtwFileActivityTracker.Instance.IsSuppressedPublic(file.FullPath))
                        continue;

                    Console.WriteLine($"[DETECTION] File missing: {file.FullPath}");
                    PrintDetectionDetails("delete", file.FullPath);
                    EmitDetectionEvent("delete", file.FullPath);
                    CheckForRenamedFile(file);
                    continue;
                }

                var info = new FileInfo(file.FullPath);

                bool hasBaseline = file.LastObservedSize != 0 ||
                                   file.LastObservedWriteTime != DateTime.MinValue ||
                                   !string.IsNullOrEmpty(file.LastObservedHash);

                if (!hasBaseline) continue;

                if (file.LastObservedSize != info.Length)
                {
                    Console.WriteLine($"[DETECTION] File size changed: {file.FullPath}");
                    Console.WriteLine($"  Expected: {file.LastObservedSize} bytes, Found: {info.Length} bytes");
                }

                if (file.LastObservedWriteTime != info.LastWriteTime)
                {
                    Console.WriteLine($"[DETECTION] File modified externally: {file.FullPath}");
                    Console.WriteLine($"  Expected: {file.LastObservedWriteTime}, Found: {info.LastWriteTime}");
                }

                string currentHash = ComputeFileHash(file.FullPath);
                if (file.LastObservedHash != currentHash)
                {
                    // Skip if the agent itself made this change.
                    if (EtwFileActivityTracker.Instance.IsSuppressedPublic(file.FullPath))
                        continue;

                    Console.WriteLine($"[DETECTION] File content changed: {file.FullPath}");
                    Console.WriteLine($"  Expected hash: {file.LastObservedHash}");
                    Console.WriteLine($"  Current hash:  {currentHash}");

                    PrintDetectionDetails("modify", file.FullPath);
                    EmitDetectionEvent("modify", file.FullPath);
                }
            }
        }

        // Look for renamed files with same hash using edit-distance similarity.
        private static void CheckForRenamedFile(CanaryFileState missingFile)
        {
            if (string.IsNullOrEmpty(missingFile.LastObservedHash)) return;

            string? directory = Path.GetDirectoryName(missingFile.FullPath);
            if (directory == null || !Directory.Exists(directory)) return;

            string missingFileName = Path.GetFileName(missingFile.FullPath);
            var candidates = new List<(string path, int similarity)>();

            foreach (var candidatePath in Directory.GetFiles(directory))
            {
                if (candidatePath == missingFile.FullPath) continue;

                try
                {
                    string candidateHash = ComputeFileHash(candidatePath);
                    if (candidateHash == missingFile.LastObservedHash)
                    {
                        string candidateFileName = Path.GetFileName(candidatePath);
                        int similarity = NormalisedEditSimilarity(missingFileName, candidateFileName);
                        candidates.Add((candidatePath, similarity));
                    }
                }
                catch { }
            }

            if (candidates.Count == 0) return;

            var bestMatch = candidates.OrderByDescending(c => c.similarity).First();

            Console.WriteLine("[DETECTION] Possible rename detected:");
            Console.WriteLine($"  Original: {missingFileName}");
            Console.WriteLine($"  Found:    {Path.GetFileName(bestMatch.path)}");

            PrintDetectionDetails("rename", bestMatch.path);
            EmitDetectionEvent("rename", bestMatch.path);

            if (candidates.Count > 1)
                Console.WriteLine($"[WARNING] {candidates.Count} files with identical content found!");
        }

        // Returns a 0–100 similarity score using normalised edit distance.
        // Higher = more similar. Replaces the old prefix-only character count.
        private static int NormalisedEditSimilarity(string a, string b)
        {
            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();

            if (a == b) return 100;
            if (a.Length == 0 || b.Length == 0) return 0;

            int maxLen = Math.Max(a.Length, b.Length);
            int dist   = LevenshteinDistance(a, b);
            return (int)((1.0 - (double)dist / maxLen) * 100);
        }

        private static int LevenshteinDistance(string a, string b)
        {
            int m = a.Length, n = b.Length;
            var dp = new int[m + 1, n + 1];

            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;

            for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i, j - 1], dp[i - 1, j]));

            return dp[m, n];
        }

        public static string ComputeFileHash(string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(sha256.ComputeHash(stream));
        }

        private static void EmitDetectionEvent(string detectionType, string path)
        {
            var activity = EtwFileActivityTracker.Instance.TryGetRecent(path, TimeSpan.FromSeconds(10));

            DetectionEtwEventSource.Log.Detection(
                activity?.ProcessId  ?? -1,
                activity?.ProcessName ?? "unknown",
                detectionType,
                path,
                DateTime.UtcNow.Ticks);
        }

        private static void PrintDetectionDetails(string detectionType, string path)
        {
            var activity = EtwFileActivityTracker.Instance
                .TryGetRecent(path, TimeSpan.FromSeconds(60));

            int    pid         = activity?.ProcessId  ?? -1;
            string processName = activity?.ProcessName ?? "unknown";

            Console.WriteLine("\n===== DETECTION ALERT =====");
            Console.WriteLine($"Type        : {detectionType}");
            Console.WriteLine($"File        : {path}");
            Console.WriteLine($"Process ID  : {pid}");
            Console.WriteLine($"Process Name: {processName}");
            Console.WriteLine($"Time        : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("===========================\n");
        }
    }
}
