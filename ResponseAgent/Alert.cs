using antiRansomware.ProcessMonitor;
using System;
using System.Collections.Generic;

namespace antiRansomware.ResponseAgent
{
    public class Alert
    {
        public string AlertId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hostname { get; set; }
        public string Source { get; set; }
        public List<MatchedRule> MatchedRules { get; set; }
        public string Severity { get; set; }
        public ProcessInfo Process { get; set; }
        public string ResponseTaken { get; set; }

        public Alert()
        {
            AlertId = Guid.NewGuid().ToString();
            Hostname = Environment.MachineName;
            Source = "process_monitor";
            MatchedRules = new List<MatchedRule>();
            Severity = "medium";
            ResponseTaken = "";
        }

        public void Display()
        {
            // Color based on severity
            string sev = Severity.ToLower();

            if (sev == "critical")
                Console.ForegroundColor = ConsoleColor.Red;
            else if (sev == "high")
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (sev == "medium")
                Console.ForegroundColor = ConsoleColor.Cyan;
            else
                Console.ForegroundColor = ConsoleColor.White;

            string line = new string('=', 70);

            Console.WriteLine("\n" + line);
            Console.WriteLine("ALERT [" + Severity.ToUpper() + "] - " + AlertId);
            Console.WriteLine("Time     : " + Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            Console.WriteLine("Host     : " + Hostname);
            Console.WriteLine("Process  : " + Process.ProcessName + " (PID: " + Process.ProcessId + ")");
            Console.WriteLine($"Parent   : {Process.ParentProcessName} (PID: {Process.ParentProcessId})");
            Console.WriteLine("Parent   : " + Process.ParentProcessId);
            Console.WriteLine("Image    : " + Process.ImageFileName);
            Console.WriteLine("Command  : " + Process.CommandLine);
            Console.WriteLine("\nMatched Rules:");

            for (int i = 0; i < MatchedRules.Count; i++)
            {
                MatchedRule match = MatchedRules[i];
                Console.WriteLine("   - [" + match.Rule.RuleId + "] " + match.Rule.Name);
                Console.WriteLine("     " + match.Reason);
            }

            Console.WriteLine("\nResponse : " + ResponseTaken);
            Console.WriteLine(line + "\n");
            Console.ResetColor();
        }
        public void LogToFile()
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "detection_log.txt");


            // Log the alert details
            string alertEntry = $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | ALERT | {Severity} | {Process.ProcessName} | PID:{Process.ProcessId} | ParentPID:{Process.ParentProcessId} | Rule:{MatchedRules[0].Rule.RuleId} | Cmd:{Process.CommandLine}";
            File.AppendAllText(logPath, alertEntry + Environment.NewLine);

            // Also log matched rules
            foreach (var match in MatchedRules)
            {
                string ruleEntry = $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | MATCHED_RULE | {match.Rule.RuleId} | {match.Reason}";
                File.AppendAllText(logPath, ruleEntry + Environment.NewLine);
            }
        }
    }
}
