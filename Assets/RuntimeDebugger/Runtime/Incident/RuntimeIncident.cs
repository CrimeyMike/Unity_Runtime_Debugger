using System;
using System.Collections.Generic;

namespace RuntimeDebugger
{
    /// <summary>
    /// Serializable exception information for incident reports.
    /// </summary>
    [Serializable]
    public class ExceptionInfo
    {
        public string ExceptionType;
        public string Message;
        public string StackTrace;
        public string Source;
    }

    /// <summary>
    /// Serializable metadata for an incident export.
    /// </summary>
    [Serializable]
    public class IncidentMetadata
    {
        public string UnityVersion;
        public string ScenePath;
        public string ExportTimestamp;
        public string DebuggerMode;
        public int TotalTraceNodes;
        public int TotalEvents;
        public int TotalLifecycleRecords;
        public int TotalAsyncRecords;
        public int TotalResourceRecords;
        public int TotalStateSnapshots;
        public int TotalMetrics;
    }

    /// <summary>
    /// Serializable incident summary for incident.json.
    /// </summary>
    [Serializable]
    public class IncidentSummary
    {
        public string Type;
        public int TriggerFrame;
        public long TriggerTimestampMs;
        public string TriggerDescription;
        public int PreTraceCount;
        public int PostTraceCount;
        public int EventCount;
        public int LifecycleCount;
        public int AsyncCount;
        public int ResourceCount;
        public int StateCount;
        public int MetricCount;
    }

    /// <summary>
    /// Complete runtime incident data. Assembled by IncidentBuilder from frozen buffers.
    /// </summary>
    [Serializable]
    public class RuntimeIncident
    {
        public IncidentType Type;
        public int TriggerFrame;
        public long TriggerTimestampMs;
        public string TriggerDescription;

        public List<TraceNode> PreTrace = new List<TraceNode>();
        public List<TraceNode> PostTrace = new List<TraceNode>();
        public List<RuntimeEvent> Events = new List<RuntimeEvent>();
        public List<LifecycleRecord> LifecycleRecords = new List<LifecycleRecord>();
        public List<AsyncTraceRecord> AsyncRecords = new List<AsyncTraceRecord>();
        public List<ResourceRecord> ResourceRecords = new List<ResourceRecord>();
        public List<StateSnapshot> StateSnapshots = new List<StateSnapshot>();
        public List<PerfMetric> Metrics = new List<PerfMetric>();
        public ExceptionInfo Exception;

        public IncidentSummary ToSummary()
        {
            return new IncidentSummary
            {
                Type = Type.ToString(),
                TriggerFrame = TriggerFrame,
                TriggerTimestampMs = TriggerTimestampMs,
                TriggerDescription = TriggerDescription,
                PreTraceCount = PreTrace.Count,
                PostTraceCount = PostTrace.Count,
                EventCount = Events.Count,
                LifecycleCount = LifecycleRecords.Count,
                AsyncCount = AsyncRecords.Count,
                ResourceCount = ResourceRecords.Count,
                StateCount = StateSnapshots.Count,
                MetricCount = Metrics.Count
            };
        }
    }
}
