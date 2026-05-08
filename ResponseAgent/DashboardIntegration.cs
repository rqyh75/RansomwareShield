using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using antiRansomware.ProcessMonitor;

namespace antiRansomware.ResponseAgent
{
    public class DashboardAlert
    {
        public string timestamp { get; set; } = "";
        public string hostname { get; set; } = "";
        public string source { get; set; } = "";
        public string severity { get; set; } = "";
        public string rule_name { get; set; } = "";
        public string response_taken { get; set; } = "";
        public Dictionary<string, object> data { get; set; } = new();
    }

    public static class DashboardIntegration
    {
        private static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private const string DashboardApiUrl = "http://localhost:5000/api/events";

        public static async Task SendAsync(DashboardAlert alert)
        {
            try
            {
                string json = JsonSerializer.Serialize(alert);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(DashboardApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Dashboard] Failed to send alert. HTTP {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dashboard] Error sending alert: {ex.Message}");
            }
        }

        public static DashboardAlert FromEtwAlert(Alert alert)
        {
            return new DashboardAlert
            {
                timestamp = alert.Timestamp.ToUniversalTime().ToString("o"),
                hostname = alert.Hostname ?? Environment.MachineName,
                source = "etw",
                severity = SafeLower(alert.Severity, "medium"),
                rule_name = GetRuleName(alert),
                response_taken = SafeLower(alert.ResponseTaken, "logged_only"),
                data = new Dictionary<string, object>
                {
                    ["process_name"] = alert.Process?.ProcessName ?? "",
                    ["parent_process_name"] = alert.Process?.ParentProcessName ?? "",
                    ["command_line"] = alert.Process?.CommandLine ?? ""
                }
            };
        }

        public static DashboardAlert FromCanaryAlert(CanaryAlert alert, string responseTaken = "terminate_process")
        {
            return new DashboardAlert
            {
                timestamp = alert.Timestamp.ToUniversalTime().ToString("o"),
                hostname = Environment.MachineName,
                source = "canary",
                severity = "critical",
                rule_name = "Canary File Access",
                response_taken = SafeLower(responseTaken, "terminate_process"),
                data = new Dictionary<string, object>
                {
                    ["process_name"] = alert.ProcessName ?? "",
                    ["parent_process_name"] = alert.ParentProcessName ?? "",
                    ["action"] = alert.Action ?? "",
                    ["canary_file_path"] = alert.CanaryFile ?? ""
                }
            };
        }

        public static DashboardAlert FromMinifilterNotification(MinifilterNotification msg)
        {
            return new DashboardAlert
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                hostname = Environment.MachineName,
                source = "minifilter",
                severity = GetMinifilterSeverity(msg.Action, msg.Response),
                rule_name = GetMinifilterRuleName(msg.Action),
                response_taken = SafeLower(msg.Response, "logged_only"),
                data = new Dictionary<string, object>
                {
                    ["process_name"] = msg.ProcessName ?? "",
                    ["parent_process_name"] = "",
                    ["action"] = msg.Action ?? "",
                    ["target_path"] = msg.TargetPath ?? ""
                }
            };
        }

        public static DashboardAlert FromMinifilterEvent(
            string timestamp,
            string hostname,
            string severity,
            string ruleName,
            string responseTaken,
            string processName,
            string parentProcessName,
            string oldFilePath,
            string newFilePath,
            string fileExtension)
        {
            return new DashboardAlert
            {
                timestamp = timestamp,
                hostname = hostname,
                source = "minifilter",
                severity = SafeLower(severity, "high"),
                rule_name = ruleName,
                response_taken = SafeLower(responseTaken, "terminate_process"),
                data = new Dictionary<string, object>
                {
                    ["process_name"] = processName ?? "",
                    ["parent_process_name"] = parentProcessName ?? "",
                    ["old_file_path"] = oldFilePath ?? "",
                    ["new_file_path"] = newFilePath ?? "",
                    ["file_extension"] = fileExtension ?? ""
                }
            };
        }

        private static string GetRuleName(Alert alert)
        {
            if (alert?.MatchedRules != null && alert.MatchedRules.Count > 0)
            {
                return alert.MatchedRules[0].Rule?.Name ?? "Unknown Rule";
            }

            return "Unknown Rule";
        }

        private static string GetMinifilterSeverity(string action, string response)
        {
            if (response?.ToLowerInvariant() == "block")
                return "critical";

            if (action?.Contains("ransom", StringComparison.OrdinalIgnoreCase) == true)
                return "critical";

            return "high";
        }

        private static string GetMinifilterRuleName(string action)
        {
            return action switch
            {
                "blocked_file_access" => "Blocked Sensitive File Access",
                "too_many_writes" => "Mass File Writes Detected",
                "rename_to_ransomware_extension" => "Ransomware File Extension Detected",
                "ransom_note_create" => "Ransom Note Creation",
                _ => "Minifilter Detection"
            };
        }

        private static string SafeLower(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.ToLowerInvariant();
        }
    }
}
