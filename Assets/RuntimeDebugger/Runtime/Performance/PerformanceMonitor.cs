using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Performance monitor using Unity ProfilerRecorder API.
    /// Samples metrics every N frames into a ring buffer.
    /// Supports Performance Mode (low-frequency) and Deep Debug Mode (per-frame).
    /// </summary>
    public sealed class PerformanceMonitor
    {
        private readonly RingBuffer<PerfMetric> _buffer;
        private readonly List<MetricDefinition> _definitions = new List<MetricDefinition>();
        private readonly Dictionary<int, ProfilerRecorder> _recorders = new Dictionary<int, ProfilerRecorder>();
        private bool _sampling;
        private int _sampleEveryNFrames = 1;
        private int _frameCounter;

        public int MetricCount => _definitions.Count;
        public int SampleCount => _buffer.Count;
        public bool IsFrozen => _buffer.IsFrozen;
        public bool IsSampling => _sampling;

        public IReadOnlyList<MetricDefinition> Definitions => _definitions;

        public PerformanceMonitor(int capacity)
        {
            _buffer = new RingBuffer<PerfMetric>(capacity);
        }

        /// <summary>
        /// Register a built-in metric. Call before StartSampling.
        /// </summary>
        public void RegisterMetric(MetricDefinition def)
        {
            if (_recorders.ContainsKey(def.Id))
                return;

            _definitions.Add(def);

            // Create ProfilerRecorder — works in Editor and Development builds
            var recorder = new ProfilerRecorder(def.ProfilerName, 1);
            _recorders[def.Id] = recorder;
        }

        /// <summary>
        /// Start sampling. In Performance Mode, set sampleEveryNFrames > 1 for lower overhead.
        /// </summary>
        public void StartSampling(int sampleEveryNFrames = 1)
        {
            if (_sampling) return;
            _sampleEveryNFrames = Mathf.Max(1, sampleEveryNFrames);
            _sampling = true;
            _frameCounter = 0;
        }

        public void StopSampling()
        {
            _sampling = false;
        }

        /// <summary>
        /// Called every frame by RuntimeDebugger. Samples metrics at the configured frequency.
        /// </summary>
        internal void OnFrameUpdate(int frame, long timestampMs)
        {
            if (!_sampling) return;

            _frameCounter++;
            if (_frameCounter < _sampleEveryNFrames)
                return;
            _frameCounter = 0;

            for (int i = 0; i < _definitions.Count; i++)
            {
                var def = _definitions[i];
                if (_recorders.TryGetValue(def.Id, out var recorder))
                {
                    double value = recorder.Valid ? recorder.LastValue : 0;
                    _buffer.Write(PerfMetric.Create(frame, timestampMs, def.Id, value));
                }
            }
        }

        public PerfMetric[] GetMetrics()
        {
            return _buffer.GetAll();
        }

        public PerfMetric[] GetLastMetrics(int count)
        {
            return _buffer.GetLast(count);
        }

        /// <summary>Get all samples for a specific metric ID.</summary>
        public List<PerfMetric> GetMetricsForId(int metricId)
        {
            var all = _buffer.GetAll();
            var result = new List<PerfMetric>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].MetricId == metricId)
                    result.Add(all[i]);
            }
            return result;
        }

        /// <summary>Get the most recent value for a metric.</summary>
        public double GetLatestValue(int metricId)
        {
            var all = _buffer.GetLast(_definitions.Count);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                if (all[i].MetricId == metricId)
                    return all[i].Value;
            }
            return 0;
        }

        public void Freeze() => _buffer.Freeze();
        public void Unfreeze() => _buffer.Unfreeze();

        public void Clear()
        {
            _buffer.Clear();
            _frameCounter = 0;
        }

        public void Dispose()
        {
            foreach (var kvp in _recorders)
                kvp.Value.Dispose();
            _recorders.Clear();
            _definitions.Clear();
            _sampling = false;
        }

        // ── Built-in metric presets ────────────────────────────────

        public static MetricDefinition FrameTimeDef => new MetricDefinition(MetricIds.FrameTime, "FrameTime", "FrameTime", MetricUnit.Milliseconds);
        public static MetricDefinition CPUTimeDef => new MetricDefinition(MetricIds.CPUTime, "CPUTime", "CPUTime", MetricUnit.Milliseconds);
        public static MetricDefinition GPUTimeDef => new MetricDefinition(MetricIds.GPUTime, "GPUTime", "GPUTime", MetricUnit.Milliseconds);
        public static MetricDefinition GCAllocDef => new MetricDefinition(MetricIds.GCAlloc, "GCAlloc", "GC.Alloc", MetricUnit.Bytes);
        public static MetricDefinition ManagedMemoryDef => new MetricDefinition(MetricIds.ManagedMemory, "ManagedMemory", "GC.MemInUseByEngine", MetricUnit.Bytes);
        public static MetricDefinition DrawCallsDef => new MetricDefinition(MetricIds.DrawCalls, "DrawCalls", "Draw Calls Count", MetricUnit.Count);
        public static MetricDefinition SetPassDef => new MetricDefinition(MetricIds.SetPass, "SetPass", "SetPass Calls Count", MetricUnit.Count);
        public static MetricDefinition BatchesDef => new MetricDefinition(MetricIds.Batches, "Batches", "Batches Count", MetricUnit.Count);

        /// <summary>Register the default set of performance metrics.</summary>
        public void RegisterDefaultMetrics()
        {
            RegisterMetric(FrameTimeDef);
            RegisterMetric(CPUTimeDef);
            RegisterMetric(GPUTimeDef);
            RegisterMetric(GCAllocDef);
            RegisterMetric(ManagedMemoryDef);
            RegisterMetric(DrawCallsDef);
            RegisterMetric(SetPassDef);
            RegisterMetric(BatchesDef);
        }
    }
}
