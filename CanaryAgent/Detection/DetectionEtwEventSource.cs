using System;
using System.Diagnostics.Tracing;

namespace CanaryAgent.Detection
{
    [EventSource(Name = "CanaryAgent-Detection")]
    public sealed class DetectionEtwEventSource : EventSource
    {
        public static readonly DetectionEtwEventSource Log = new();

        private DetectionEtwEventSource() { }

        [Event(
            1,
            Level = EventLevel.Warning,
            Message = "DetectionType={2}; ProcessId={0}; ProcessName={1}; Path={3}; UtcTicks={4}")]
        public void Detection(
            int processId,
            string processName,
            string detectionType,
            string path,
            long utcTicks)
        {
            WriteEvent(1, processId, processName ?? "unknown", detectionType, path, utcTicks);
        }
    }
}