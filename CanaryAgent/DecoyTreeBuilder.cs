using CanaryAgent.Actors;
using CanaryAgent.DataGen;
using CanaryAgent.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CanaryAgent.Core
{
    internal static class DecoyTreeBuilder
    {
        public static List<string> CreateDecoyTree(string root)
        {
            Directory.CreateDirectory(root);

            var createdDirs = new List<string>();

            void AddDir(string path)
            {
                Directory.CreateDirectory(path);
                if (!createdDirs.Contains(path, StringComparer.OrdinalIgnoreCase))
                    createdDirs.Add(path);
            }

            AddDir(root);

            string[] topLevel =
            {
                "Finance", "HR", "Legal", "Sales", "Operations",
                "Projects", "Reports", "Archive", "Temp"
            };

            var structure = new Dictionary<string, string[]>
            {
                ["Finance"]    = new[] { "Accounts", "Invoices", "Budgets", "Payroll", "Q1", "Q2", "Q3", "Q4", "Archive" },
                ["HR"]         = new[] { "Recruitment", "Employees", "Benefits", "Policies", "2025", "2026", "Archive" },
                ["Legal"]      = new[] { "Contracts", "Vendors", "Policies", "Cases", "Archive" },
                ["Sales"]      = new[] { "Leads", "Customers", "Pipeline", "Regions", "Archive" },
                ["Operations"] = new[] { "Inventory", "Suppliers", "Logistics", "Warehouse", "Archive" },
                ["Projects"]   = new[] { "Alpha", "Beta", "Migration", "Internal", "Drafts", "Final" },
                ["Reports"]    = new[] { "Weekly", "Monthly", "Quarterly", "Board", "Archive" },
                ["Archive"]    = new[] { "2023", "2024", "2025", "2026" },
                ["Temp"]       = new[] { "Exports", "Uploads", "Logs", "Staging" }
            };

            foreach (var top in topLevel)
            {
                string topPath = Path.Combine(root, top);
                AddDir(topPath);

                if (!structure.TryGetValue(top, out var subs)) continue;

                foreach (var sub in subs)
                {
                    string subPath = Path.Combine(topPath, sub);
                    AddDir(subPath);

                    if (top == "Finance" && (sub == "Q1" || sub == "Q2" || sub == "Q3" || sub == "Q4"))
                    {
                        AddDir(Path.Combine(subPath, "Drafts"));
                        AddDir(Path.Combine(subPath, "Final"));
                    }

                    if (top == "Projects" && (sub == "Alpha" || sub == "Beta" || sub == "Migration"))
                    {
                        AddDir(Path.Combine(subPath, "Docs"));
                        AddDir(Path.Combine(subPath, "Exports"));
                    }

                    if (top == "Temp" && sub == "Logs")
                        AddDir(Path.Combine(subPath, "Old"));
                }
            }

            return createdDirs;
        }

        public static void SeedDecoyFiles(
            string root,
            StateStore state,
            FileActor fileActor,
            DateTime now,
            Random rng)
        {
            var allDirs = CreateDecoyTree(root);

            var folderMap = new Dictionary<PersonaType, List<string>>
            {
                [PersonaType.Finance_Accounting]   = allDirs.Where(d => d.Contains(@"\Finance",    StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.HR_Payroll]           = allDirs.Where(d => d.Contains(@"\HR",         StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.Legal_Contracts]      = allDirs.Where(d => d.Contains(@"\Legal",      StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.Sales_CRM]            = allDirs.Where(d => d.Contains(@"\Sales",      StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.Operations_Inventory] = allDirs.Where(d => d.Contains(@"\Operations", StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.Executive_Reports]    = allDirs.Where(d => d.Contains(@"\Reports",    StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.IT_Credentials]       = allDirs.Where(d => d.Contains(@"\Projects",   StringComparison.OrdinalIgnoreCase)
                                                                     || d.Contains(@"\Temp",       StringComparison.OrdinalIgnoreCase)).ToList(),
                [PersonaType.System_Logs]          = allDirs.Where(d => d.Contains(@"\Temp",       StringComparison.OrdinalIgnoreCase)
                                                                     || d.Contains(@"\Reports",    StringComparison.OrdinalIgnoreCase)).ToList()
            };

            var seedPlan = new Dictionary<PersonaType, int>
            {
                [PersonaType.Finance_Accounting]   = 6,
                [PersonaType.HR_Payroll]           = 4,
                [PersonaType.Legal_Contracts]      = 3,
                [PersonaType.Sales_CRM]            = 4,
                [PersonaType.Operations_Inventory] = 4,
                [PersonaType.Executive_Reports]    = 2,
                [PersonaType.IT_Credentials]       = 2,
                [PersonaType.System_Logs]          = 3
            };

            foreach (var kvp in seedPlan)
            {
                var  type  = kvp.Key;
                int  count = kvp.Value;

                if (!folderMap.TryGetValue(type, out var possibleDirs) || possibleDirs.Count == 0)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    string targetDir = possibleDirs[rng.Next(possibleDirs.Count)];
                    var    created   = fileActor.CreateFileFromPersonaType(targetDir, now, state.Files, type);
                    if (created == null) break;

                    created.NextModificationTime = now.AddMinutes(rng.Next(45, 241));
                    created.NextRenameTime       = now.AddHours(rng.Next(24, 97));
                    state.Files.Add(created);
                }
            }
        }
    }
}
