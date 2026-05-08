using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace antiRansomware.ProcessMonitor
{
    public class ETWProcessMonitor : IDisposable
    {
        private TraceEventSession? session;
        private bool isRunning = false;

        // Event that the Response Agent will subscribe to
        public event Action<ProcessInfo>? OnProcessDetected;

        //Implementation is here:
        public void Start()
        {
            if (isRunning) return;

            // step1: Create a unique session name to avoid conflicts
            string sessionName = $"ProcessMonitorETW";

            try
            {
                // step 2: Open/Create a real-time session (no file output) that's why null
                // This session is a 'controller' which can turn ETW providers on and off. 
                session = new TraceEventSession(sessionName, null);

                // step 3: Enable the Kernel Process provider (Kernal provider is a type of ETW providers)
                // The keyword 0x10 enables process start/stop events [9]
                session.EnableKernelProvider(
                    KernelTraceEventParser.Keywords.Process |
                    KernelTraceEventParser.Keywords.ImageLoad
                );

                // step 4: Create an event source that reads from our session
                var source = new ETWTraceEventSource(sessionName, TraceEventSourceType.Session);

                // step5: Connect the kernel parser
                var kernelParser = new KernelTraceEventParser(source);

                // Subscribe to Process Start events
                kernelParser.ProcessStart += (ProcessTraceData data) =>
                {
                    var processInfo = ExtractProcessInfo(data);
                    OnProcessDetected?.Invoke(processInfo);
                };

                // Start processing events asynchronously
                // Process Method is a blocking call — it loops forever, continuously pulling ETW events from the kernel
                // It needs a thread to work in the background not to freeze the main application 
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        source.Process(); // This blocks until Stop is called
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ETW processing error: {ex.Message}");
                    }
                });

                isRunning = true;
                Debug.WriteLine("Process Monitor started successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start ETW session: {ex.Message}");
                session?.Dispose();
                session = null;
                throw;
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            try
            {
                if (session != null) { session.Stop(); }
                if (session != null) { session.Dispose(); }
                session = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping ETW session: {ex.Message}");
            }

            isRunning = false;
            Debug.WriteLine("Process Monitor stopped");
        }

        public void Dispose()
        {
            Stop();
        }
        private string GetProcessName(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return process.ProcessName;
            }
            catch
            {
                return "unknown";
            }
        }
        private ProcessInfo ExtractProcessInfo(ProcessTraceData data)
        {
            var info = new ProcessInfo
            {
                ProcessId = data.ProcessID,
                ParentProcessId = data.ParentID,
                Timestamp = DateTime.Now,
                ImageFileName = data.ImageFileName ?? string.Empty, //get the image file name, if not found use empty string
                ProcessName = data.ProcessName ?? string.Empty, //get the process name, if not found use empty string
                CommandLine = data.CommandLine ?? string.Empty //get the command line, if not found use empty string
              
            };

            // Get parent process name
            info.ParentProcessName = GetProcessName(info.ParentProcessId);

            //if process name is empty and image file name is not empty, set the process name to be similar to the image file name 
            //process name is essential for response agent and forensics logs 
            if (string.IsNullOrEmpty(info.ProcessName) && !string.IsNullOrEmpty(info.ImageFileName))
            {
                info.ProcessName = System.IO.Path.GetFileName(info.ImageFileName);
            }

            return info;
        }
    }

    // Data structure for sending to Response Agent
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public int ParentProcessId { get; set; }
        public string ParentProcessName { get; set; } = string.Empty; 

        public string ProcessName { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public string ImageFileName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Username { get; set; }
    }

}


//[9] https://learn.microsoft.com/kk-kz/archive/blogs/vancem/using-traceevent-to-mine-information-in-os-registered-etw-providers