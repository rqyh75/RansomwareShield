using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanaryAgent.Communication
{
    public class CanaryAlert
    {
        public string Source { get; set; } = "canary";
        public DateTime Timestamp { get; set; }
        public string CanaryFile { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public int ParentProcessId { get; set; }
        public string ParentProcessName { get; set; } = string.Empty;
        public string? Username { get; set; }
    }
}