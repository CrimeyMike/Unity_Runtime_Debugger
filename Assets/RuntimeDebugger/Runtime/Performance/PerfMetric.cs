using System;

namespace RuntimeDebugger
{
    public enum MetricUnit
    {
        Milliseconds,
        Bytes,
        Count
    }

    /// <summary>
    /// Definition of a performance metric. Maps to a Unity ProfilerRecorder name.
    /// </summary>
    [Serializable]
    public readonly struct MetricDefinition
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string ProfilerName;
        public readonly MetricUnit Unit;

        public MetricDefinition(int id, string name, string profilerName, MetricUnit unit)
        {
            Id = id;
            Name = name;
            ProfilerName = profilerName;
            Unit = unit;
        }
    }

    /// <summary>
    /// Predefined metric IDs for fast lookup.
    /// </summary>
    public static class MetricIds
    {
        public const int FrameTime = 1;
        public const int CPUTime = 2;
        public const int GPUTime = 3;
        public const int GCAlloc = 4;
        public const int ManagedMemory = 5;
        public const int DrawCalls = 6;
        public const int SetPass = 7;
        public const int Batches = 8;
        public const int PendingAsyncTasks = 9;
        public const int PendingAssetLoads = 10;
    }

    /// <summary>
    /// A single performance metric sample.
    /// </summary>
    [Serializable]
    public struct PerfMetric
    {
        public int Frame;
        public long TimestampMs;
        public int MetricId;
        public double Value;

        public static PerfMetric Create(int frame, long timestampMs, int metricId, double value)
        {
            return new PerfMetric
            {
                Frame = frame,
                TimestampMs = timestampMs,
                MetricId = metricId,
                Value = value
            };
        }
    }
}
