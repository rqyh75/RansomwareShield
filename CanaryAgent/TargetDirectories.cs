using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CanaryAgent.Core
{
    internal static class TargetDirectories
    {
        public static List<string> GetWritableCandidateDirectories()
        {
            var candidates = new List<string>();

            void AddIfValid(Environment.SpecialFolder sf)
            {
                var p = Environment.GetFolderPath(sf);
                if (!string.IsNullOrWhiteSpace(p))
                    candidates.Add(p);
            }

            // High-value user locations
            AddIfValid(Environment.SpecialFolder.MyDocuments);
            AddIfValid(Environment.SpecialFolder.DesktopDirectory);

            // Shared/public (often writable, depends on org policy)
            AddIfValid(Environment.SpecialFolder.CommonDocuments);

            // AppData (writable, but “documents-like” realism is lower)
            AddIfValid(Environment.SpecialFolder.ApplicationData);       // Roaming
            AddIfValid(Environment.SpecialFolder.LocalApplicationData);  // Local

            // Optionally: OneDrive if present (very realistic for orgs)
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                var oneDrive = Directory.GetDirectories(userProfile, "OneDrive*", SearchOption.TopDirectoryOnly)
                                        .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(oneDrive))
                    candidates.Add(oneDrive);
            }

            // De-dup + filter to writable + existing
            return candidates
                .Select(Normalize)
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(IsWritableDirectory)
                .ToList();
        }

        public static string PickRandomDirectory(Random rng, List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                throw new InvalidOperationException("No writable candidate directories found.");

            Console.WriteLine($"✓ Directory Picking: {candidates[rng.Next(candidates.Count)]}");
            

            return candidates[rng.Next(candidates.Count)];
        }

        private static bool IsWritableDirectory(string dir)
        {
            try
            {
                var test = Path.Combine(dir, $".canary_write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(test, "test");
                File.Delete(test);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Normalize(string p) => Path.GetFullPath(p.Trim());
    }
}