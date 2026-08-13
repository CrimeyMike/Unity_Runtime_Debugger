using System.Collections.Generic;
using Conditional = System.Diagnostics.ConditionalAttribute;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Static facade API for the Unity Runtime Debugger.
    /// Call Initialize() once at startup, then use RecordEvent/Trace throughout your code.
    /// </summary>
    public static class RuntimeDebugger
    {
        private const int DefaultTraceCapacity = 4096;
        private const int DefaultEventCapacity = 4096;
        private const int DefaultMaxTraceDepth = 64;
        private const int DefaultStateCapacity = 1024;
        private const int DefaultLifecycleCapacity = 1024;
        private const int DefaultAsyncCapacity = 1024;
        private const int DefaultResourceCapacity = 1024;
        private const int DefaultPerfCapacity = 8192;
        private const int PerfSampleEveryNFrames_Performance = 10;

        private static TraceTree s_traceTree;
        private static RingBuffer<RuntimeEvent> s_eventBuffer;
        private static DebugSession s_session;
        private static StateRegistry s_stateRegistry;
        private static LifecycleTracker s_lifecycleTracker;
        private static AsyncTracker s_asyncTracker;
        private static ResourceTracker s_resourceTracker;
        private static PerformanceMonitor s_perfMonitor;
        private static TriggerSystem s_triggerSystem;
        private static int s_currentFrame;
        private static bool s_initialized;
        private static bool s_enabled;

        private static readonly object s_nameLock = new object();
        private static readonly Dictionary<int, string> s_hashToName = new Dictionary<int, string>();

        public static bool IsEnabled => s_enabled;
        public static bool IsInitialized => s_initialized;
        public static DebugMode Mode => s_session?.Mode ?? DebugMode.TriggerDebug;
        public static int CurrentFrame => s_currentFrame;
        public static int EventCount => s_eventBuffer?.Count ?? 0;
        public static int TraceCount => s_traceTree?.CompletedCount ?? 0;

        public static StateRegistry State => s_stateRegistry;
        public static LifecycleTracker Lifecycle => s_lifecycleTracker;
        public static AsyncTracker Async => s_asyncTracker;
        public static ResourceTracker Resource => s_resourceTracker;
        public static PerformanceMonitor Performance => s_perfMonitor;
        public static TriggerSystem Triggers => s_triggerSystem;

        public static void Initialize(
            int traceCapacity = DefaultTraceCapacity,
            int eventCapacity = DefaultEventCapacity,
            int maxTraceDepth = DefaultMaxTraceDepth,
            int stateCapacity = DefaultStateCapacity,
            int lifecycleCapacity = DefaultLifecycleCapacity,
            int asyncCapacity = DefaultAsyncCapacity,
            int resourceCapacity = DefaultResourceCapacity,
            int perfCapacity = DefaultPerfCapacity)
        {
            s_traceTree = new TraceTree(traceCapacity, maxTraceDepth);
            s_eventBuffer = new RingBuffer<RuntimeEvent>(eventCapacity);
            s_session = new DebugSession(DebugMode.TriggerDebug);
            s_stateRegistry = new StateRegistry(stateCapacity);
            s_lifecycleTracker = new LifecycleTracker(lifecycleCapacity);
            s_asyncTracker = new AsyncTracker(asyncCapacity);
            s_resourceTracker = new ResourceTracker(resourceCapacity);
            s_perfMonitor = new PerformanceMonitor(perfCapacity);
            s_perfMonitor.RegisterDefaultMetrics();
            s_triggerSystem = new TriggerSystem();
            s_currentFrame = 0;
            s_initialized = true;
            s_enabled = true;

            // Enable zero-setup auto-instrumentation:
            // - All Debug.Log calls auto-captured as events
            // - Exception / FrameSpike / GCSpike triggers auto-registered
            // - Scene objects auto-monitored (no game code changes needed)
            AutoInstrumentation.Enable();

            // Auto-install scene monitor (creates a hidden GameObject)
            // This only works in Play Mode — in Edit Mode the Editor window handles updates
            if (Application.isPlaying)
            {
                AutoSceneMonitor.Install();
            }

            Debug.Log("[RuntimeDebugger] Initialized. Zero-code auto-instrumentation active:\n" +
                      "  ✅ Exception / FrameSpike / GCSpike triggers (auto)\n" +
                      "  ✅ Debug.Log auto-capture (auto)\n" +
                      "  ✅ Scene object lifecycle tracking (auto)\n" +
                      "  ✅ Performance metrics sampling (auto)\n" +
                      "No game code modification needed.");
        }

        public static void Shutdown()
        {
            AutoInstrumentation.Disable();
            s_traceTree?.Clear();
            s_eventBuffer?.Clear();
            s_stateRegistry?.Clear();
            s_lifecycleTracker?.Clear();
            s_asyncTracker?.Clear();
            s_resourceTracker?.Clear();
            s_perfMonitor?.Dispose();
            s_perfMonitor = null;
            s_triggerSystem = null;
            lock (s_nameLock)
                s_hashToName.Clear();
            s_session = null;
            s_initialized = false;
            s_enabled = false;
        }

        public static void SetEnabled(bool enabled)
        {
            s_enabled = enabled;
        }

        // ── Event API ──────────────────────────────────────────────

        [Conditional("RUNTIME_DEBUGGER_ENABLED")]
        public static void RecordEvent(string name)
        {
            if (!s_enabled || !s_initialized) return;

            int hash = HashUtil.HashString(name);
            RegisterName(hash, name);
            int contextId = s_traceTree?.CurrentNodeId ?? -1;
            s_eventBuffer.Write(RuntimeEvent.Create(s_currentFrame, TimeUtil.NowMs(), hash, contextId));
        }

        // ── Trace API ──────────────────────────────────────────────

        public static TraceScope Trace(string name)
        {
            if (!s_enabled || !s_initialized)
                return default;

            int hash = HashUtil.HashString(name);
            RegisterName(hash, name);
            int nodeId = s_traceTree.BeginTrace(hash, s_currentFrame, TimeUtil.NowMs());
            return new TraceScope(nodeId, name);
        }

        internal static void EndTrace(int nodeId)
        {
            if (nodeId < 0 || !s_initialized) return;
            s_traceTree.EndTrace(nodeId, TimeUtil.NowMs());
        }

        // ── Lifecycle API ─────────────────────────────────────────

        [Conditional("RUNTIME_DEBUGGER_ENABLED")]
        public static void Track(object obj)
        {
            if (!s_enabled || !s_initialized || obj == null) return;
            s_lifecycleTracker.Track(obj.GetType().Name, s_currentFrame, TimeUtil.NowMs());
        }

        // ── Async API ─────────────────────────────────────────────

        [Conditional("RUNTIME_DEBUGGER_ENABLED")]
        public static void RecordAsyncStart(string operation, int ownerObjectId)
        {
            if (!s_enabled || !s_initialized) return;
            s_asyncTracker.StartTask(operation, ownerObjectId, s_currentFrame, TimeUtil.NowMs());
        }

        // ── Resource API ──────────────────────────────────────────

        [Conditional("RUNTIME_DEBUGGER_ENABLED")]
        public static void RecordResourceLoad(string assetPath, int ownerObjectId)
        {
            if (!s_enabled || !s_initialized) return;
            s_resourceTracker.RecordLoadStart(assetPath, ownerObjectId, s_currentFrame, TimeUtil.NowMs());
        }

        // ── Session API ───────────────────────────────────────────

        public static void StartSession(DebugMode mode, float durationSec = 0f)
        {
            if (!s_initialized)
            {
                Debug.LogWarning("[RuntimeDebugger] Not initialized. Call Initialize() first.");
                return;
            }

            s_session = new DebugSession(mode, durationSec);
            s_session.Start();

            // Configure performance sampling based on mode
            if (s_perfMonitor != null)
            {
                if (mode == DebugMode.Performance)
                {
                    s_perfMonitor.StartSampling(PerfSampleEveryNFrames_Performance);
                }
                else if (mode == DebugMode.DeepDebug || mode == DebugMode.TriggerDebug)
                {
                    s_perfMonitor.StartSampling(1); // every frame
                }
            }

            Debug.Log($"[RuntimeDebugger] Session started: Mode={mode}, Duration={durationSec}s");
        }

        public static void StopSession()
        {
            if (s_session?.IsActive == true)
            {
                s_session.Stop();
                Debug.Log("[RuntimeDebugger] Session stopped.");
            }
        }

        // ── Data Access (for Editor / Export) ─────────────────────

        internal static TraceNode[] GetTraceTree()
        {
            return s_traceTree?.GetTree() ?? System.Array.Empty<TraceNode>();
        }

        internal static RuntimeEvent[] GetEvents()
        {
            return s_eventBuffer?.GetAll() ?? System.Array.Empty<RuntimeEvent>();
        }

        internal static RuntimeEvent[] GetLastEvents(int count)
        {
            return s_eventBuffer?.GetLast(count) ?? System.Array.Empty<RuntimeEvent>();
        }

        internal static TraceNode[] GetLastTraces(int count)
        {
            return s_traceTree?.GetLast(count) ?? System.Array.Empty<TraceNode>();
        }

        internal static StateSnapshot[] GetStateSnapshots()
        {
            return s_stateRegistry?.GetSnapshots() ?? System.Array.Empty<StateSnapshot>();
        }

        internal static LifecycleRecord[] GetLifecycleRecords()
        {
            return s_lifecycleTracker?.GetRecords() ?? System.Array.Empty<LifecycleRecord>();
        }

        internal static AsyncTraceRecord[] GetAsyncRecords()
        {
            return s_asyncTracker?.GetRecords() ?? System.Array.Empty<AsyncTraceRecord>();
        }

        internal static ResourceRecord[] GetResourceRecords()
        {
            return s_resourceTracker?.GetRecords() ?? System.Array.Empty<ResourceRecord>();
        }

        internal static PerfMetric[] GetPerfMetrics()
        {
            return s_perfMonitor?.GetMetrics() ?? System.Array.Empty<PerfMetric>();
        }

        internal static void FreezeBuffers()
        {
            s_traceTree?.Freeze();
            s_eventBuffer?.Freeze();
            s_stateRegistry?.Freeze();
            s_lifecycleTracker?.Freeze();
            s_asyncTracker?.Freeze();
            s_resourceTracker?.Freeze();
            s_perfMonitor?.Freeze();
        }

        internal static void UnfreezeBuffers()
        {
            s_traceTree?.Unfreeze();
            s_eventBuffer?.Unfreeze();
            s_stateRegistry?.Unfreeze();
            s_lifecycleTracker?.Unfreeze();
            s_asyncTracker?.Unfreeze();
            s_resourceTracker?.Unfreeze();
            s_perfMonitor?.Unfreeze();
        }

        internal static void ClearAll()
        {
            s_traceTree?.Clear();
            s_eventBuffer?.Clear();
            s_stateRegistry?.Clear();
            s_lifecycleTracker?.Clear();
            s_asyncTracker?.Clear();
            s_resourceTracker?.Clear();
            s_perfMonitor?.Clear();
        }

        public static string GetEventName(int hash)
        {
            lock (s_nameLock)
            {
                return s_hashToName.TryGetValue(hash, out var name) ? name : $"#{hash}";
            }
        }

        // ── Internal Updates ───────────────────────────────────────

        internal static void OnFrameUpdate()
        {
            s_currentFrame = Time.frameCount;
            long nowMs = TimeUtil.NowMs();

            s_perfMonitor?.OnFrameUpdate(s_currentFrame, nowMs);
            s_triggerSystem?.OnFrameUpdate(s_currentFrame, nowMs);

            if (s_session != null && s_session.IsActive)
            {
                s_session.OnFrameUpdate();

                if (s_session.Mode == DebugMode.DeepDebug && s_session.IsExpired)
                {
                    s_session.Stop();
                    s_perfMonitor?.StopSampling();
                    Debug.Log("[RuntimeDebugger] Deep Debug session expired (auto-stop).");
                }
            }
        }

        internal static void OnException(System.Exception ex)
        {
            if (!s_enabled || !s_initialized) return;
            Debug.Log($"[RuntimeDebugger] Exception captured: {ex.GetType().Name}: {ex.Message}");
        }

        internal static void RegisterName(int hash, string name)
        {
            lock (s_nameLock)
            {
                if (!s_hashToName.ContainsKey(hash))
                    s_hashToName[hash] = name;
            }
        }
    }
}
