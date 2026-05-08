using antiRansomware.ProcessMonitor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace antiRansomware.ResponseAgent
{
    public class RuleEngine
    {
        private List<DetectionRule> rules = new();

        public void LoadRules(string rulesPath)
        {
            if (!File.Exists(rulesPath))
            {
                Console.WriteLine($"[!] Rules file not found: {rulesPath}");
                return;
            }

            string json = File.ReadAllText(rulesPath);
            var root = JsonSerializer.Deserialize<RulesRoot>(json);

            if (root?.Rules == null)
            {
                Console.WriteLine("[!] Failed to parse rules.json");
                return;
            }

            rules = new List<DetectionRule>();
            foreach (var rule in root.Rules)
            {
                if (rule.Source != "process_monitor") continue;

                // Warn at startup about rules that will never fire: they declare
                // registry_keys but have no patterns, and we only match on
                // command-line patterns and executable names at this point.
                bool hasPatterns   = rule.Patterns        is { Count: > 0 };
                bool hasExeNames   = rule.ExecutableNames is { Count: > 0 };
                bool hasRegKeys    = rule.RegistryKeys    is { Count: > 0 };

                if (hasRegKeys && !hasPatterns && !hasExeNames)
                {
                    Console.WriteLine(
                        $"[!] Rule {rule.RuleId} ({rule.Name}) declares registry_keys but has no " +
                        $"patterns or executable_names — it will never match via process monitor. " +
                        $"Add a matching 'patterns' entry or move it to a registry monitor source.");
                }

                rules.Add(rule);
            }

            Console.WriteLine($"[*] Loaded {rules.Count} process monitor rules from {rulesPath}");
        }

        public List<MatchedRule> MatchProcess(ProcessInfo processInfo)
        {
            var matches = new List<MatchedRule>();

            foreach (var rule in rules)
            {
                // Check 1: executable name
                if (rule.ExecutableNames is { Count: > 0 })
                {
                    foreach (var name in rule.ExecutableNames)
                    {
                        if (string.Equals(processInfo.ProcessName, name, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add(new MatchedRule
                            {
                                Rule   = rule,
                                Reason = $"Executable name matches: {processInfo.ProcessName}"
                            });
                            break;
                        }
                    }
                }

                // Check 2: command-line pattern
                if (rule.Patterns is { Count: > 0 } && !string.IsNullOrEmpty(processInfo.CommandLine))
                {
                    foreach (var pattern in rule.Patterns)
                    {
                        if (Regex.IsMatch(processInfo.CommandLine, pattern, RegexOptions.IgnoreCase))
                        {
                            matches.Add(new MatchedRule
                            {
                                Rule   = rule,
                                Reason = $"Command line matches pattern: {pattern}"
                            });
                            break;
                        }
                    }
                }

                // Note: registry_keys matching requires a separate registry-monitor
                // component (e.g. ETW Registry provider or a registry watcher).
                // It is not evaluated here because ETWProcessMonitor only receives
                // process-start events, which do not carry registry paths.
            }

            return matches;
        }
    }
}
