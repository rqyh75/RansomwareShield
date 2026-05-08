using System;

namespace antiRansomware
{
    /// <summary>
    /// Centralised, thread-safe console output for the unified RansomShield terminal.
    /// All child-process output (Canary Agent, Dashboard backend/frontend) is routed
    /// through <see cref="Child"/> so that every line is tagged with a coloured label.
    /// </summary>
    public static class ConsoleHub
    {
        private static readonly object LockObj = new();

        // ── Banner ────────────────────────────────────────────────────────────

        public static void Banner()
        {
            lock (LockObj)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(@"
 ____                                 ____  _     _      _     _        
|  _ \ __ _ _ __  ___  ___  _ __ ___ / ___|| |__ (_) ___| | __| |      
| |_) / _` | '_ \/ __|/ _ \| '_ ` _ \\___ \| '_ \| |/ _ \ |/ _` |  /\_/\      
|  _ < (_| | | | \__ \ (_) | | | | | |___) | | | | |  __/ | (_| | ( o.o )
|_| \_\__,_|_| |_|___/\___/|_| |_| |_|____/|_| |_|_|\___|_|\__,_|  > ^ <

        Windows-Based Early Ransomware Detection and Response System
              Using Canary Files and Behavioural Analysis
                    Arwa, Ruqaiyah, Aseel, Liya


 [*] Components:
     [+] Minifilter Driver  (fltmc load)
     [+] Dashboard API      (Spring Boot  → http://localhost:5000)
     [+] Dashboard UI       (React / npm  → http://localhost:3000)
     [+] Canary Agent       (dotnet run)
     [+] Response Agent     (this process)
");
                Console.ResetColor();
                Console.WriteLine(new string('═', 78));
            }
        }

        // ── Fixed-label helpers ───────────────────────────────────────────────

        public static void System(string message)      => Write("SYSTEM",    message, ConsoleColor.Cyan);
        public static void Success(string message)     => Write("OK",        message, ConsoleColor.Green);
        public static void Info(string message)        => Write("INFO",      message, ConsoleColor.Gray);
        public static void Warning(string message)     => Write("WARN",      message, ConsoleColor.Yellow);
        public static void Error(string message)       => Write("ERROR",     message, ConsoleColor.Red);
        public static void Canary(string message)      => Write("CANARY",    message, ConsoleColor.Magenta);
        public static void Etw(string message)        => Write("ETW",       message, ConsoleColor.Yellow);
        public static void Minifilter(string message)  => Write("FILTER",    message, ConsoleColor.Blue);
        public static void Dashboard(string message)   => Write("DASHBOARD", message, ConsoleColor.DarkCyan);
        public static void Critical(string message)    => Write("ALERT",     message, ConsoleColor.Red);

        // ── Dynamic-label helper (child process output) ───────────────────────

        /// <summary>
        /// Routes a single stdout/stderr line from a child process (Canary Agent,
        /// Dashboard backend, Dashboard frontend) into the unified console.
        /// </summary>
        /// <param name="label">Short component name, e.g. "CANARY", "DASH-API".</param>
        /// <param name="message">The line of text from the child process.</param>
        /// <param name="color">Label colour. Use a different colour per component for easy scanning.</param>
        public static void Child(string label, string message, ConsoleColor color)
            => Write(label, message, color);

        // ── Section divider ───────────────────────────────────────────────────

        public static void Section(string title, ConsoleColor color)
        {
            lock (LockObj)
            {
                Console.ForegroundColor = color;
                Console.WriteLine();
                Console.WriteLine("╔" + new string('═', 76) + "╗");
                Console.WriteLine($"║ {title.PadRight(74)} ║");
                Console.WriteLine("╚" + new string('═', 76) + "╝");
                Console.ResetColor();
            }
        }

        // ── Core write ────────────────────────────────────────────────────────

        private static void Write(string source, string message, ConsoleColor color)
        {
            lock (LockObj)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{DateTime.Now:HH:mm:ss}] ");

                Console.ForegroundColor = color;
                Console.Write($"[{source,-9}] ");

                Console.ResetColor();
                Console.WriteLine(message);
            }
        }
    }
}
