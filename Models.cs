using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace antiRansomware.ResponseAgent
{
    // This matches the root of your JSON file
    public class RulesRoot
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonPropertyName("rules")]
        public List<DetectionRule> Rules { get; set; } = new List<DetectionRule>();
    }

    // This matches each rule object in the "rules" array
    public class DetectionRule
    {
        [JsonPropertyName("rule_id")]
        public string RuleId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty; // "process_monitor", "minifilter", "canaryfiles"

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty; // "low", "medium", "high", "critical"

        
        [JsonPropertyName("patterns")]
        public List<string>? Patterns { get; set; } // For command-line pattern matching

        [JsonPropertyName("executable_names")]
        public List<string>? ExecutableNames { get; set; } // For executable name matching


        // For minifilter rules
        [JsonPropertyName("extensions")]
        public List<string>? Extensions { get; set; } 

        [JsonPropertyName("file_names")]
        public List<string>? FileNames { get; set; }

        [JsonPropertyName("operation")]
        public string? Operation { get; set; }

        [JsonPropertyName("threshold")]
        public int? Threshold { get; set; }

        [JsonPropertyName("window_seconds")]
        public int? WindowSeconds { get; set; }

        
        // For network rules (future work)

        [JsonPropertyName("ip_addresses")]
        public List<string>? IpAddresses { get; set; }

        [JsonPropertyName("domains")]
        public List<string>? Domains { get; set; }

        [JsonPropertyName("urls")]
        public List<string>? Urls { get; set; }

        [JsonPropertyName("user_agents")]
        public List<string>? UserAgents { get; set; }

        [JsonPropertyName("registry_keys")]
        public List<string>? RegistryKeys { get; set; }

        // Response action

        [JsonPropertyName("response")]
        public string Response { get; set; } = "alert_only";  // "terminate_process" or "alert_only"
    }


    // This represents a matched rule with the reason why it matched
    public class MatchedRule
    {
        public DetectionRule Rule { get; set; } = null!;
        public string Reason { get; set; } = string.Empty;
    }
}