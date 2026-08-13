using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Zero-setup automatic instrumentation.
    ///
    /// After Initialize(), this module automatically:
    /// 1. Captures ALL UnityEngine.Debug.Log / LogWarning / LogError as runtime events
    /// 2. Registers default triggers (Exception, FrameSpike, GCSpike)
    /// 3. Starts performance sampling
    ///
    /// The user gets full incident detection WITHOUT writing a single RecordEvent/Trace call.
    /// </summary>
    public static class AutoInstrumentation
    {
        private static bool s_active;
        private static ExceptionTrigger s_exceptionTrigger;
        private static FrameSpikeTrigger s_frameSpikeTrigger;
        private static GCSpikeTrigger s_gcSpikeTrigger;

        /// <summary>
        /// Enable automatic instrumentation. Called by RuntimeDebugger.Initialize().
        /// </summary>
        public static void Enable(
            float frameSpikeThresholdMs = 33.3f,
            float gcSpikeThresholdBytes = 1048576f)
        {
            if (s_active) return;
            s_active = true;

            // 1. Auto-capture all Unity logs as events
            Application.logMessageReceived += OnLogMessageReceived;

            // 2. Register default triggers
            s_exceptionTrigger = new ExceptionTrigger();
            s_frameSpikeTrigger = new FrameSpikeTrigger(frameSpikeThresholdMs);
            s_gcSpikeTrigger = new GCSpikeTrigger(gcSpikeThresholdBytes);

            if (RuntimeDebugger.Triggers != null)
            {
                RuntimeDebugger.Triggers.RegisterTrigger(s_exceptionTrigger);
                RuntimeDebugger.Triggers.RegisterTrigger(s_frameSpikeTrigger);
                RuntimeDebugger.Triggers.RegisterTrigger(s_gcSpikeTrigger);
            }

            Debug.Log("[RuntimeDebugger] Auto-instrumentation enabled. " +
                      "Exception / FrameSpike / GCSpike triggers registered automatically.");
        }

        /// <summary>
        /// Disable automatic instrumentation and clean up hooks.
        /// </summary>
        public static void Disable()
        {
            if (!s_active) return;
            s_active = false;

            Application.logMessageReceived -= OnLogMessageReceived;

            s_exceptionTrigger?.Dispose();
            s_exceptionTrigger = null;
            s_frameSpikeTrigger = null;
            s_gcSpikeTrigger = null;
        }

        /// <summary>Is auto-instrumentation currently active?</summary>
        public static bool IsActive => s_active;

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!RuntimeDebugger.IsEnabled || !RuntimeDebugger.IsInitialized) return;

            // Auto-capture as runtime events — these go into the ring buffer
            // and will be included in any incident capture
            string eventName;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    eventName = "Log.Error";
                    break;
                case LogType.Warning:
                    eventName = "Log.Warning";
                    break;
                default:
                    eventName = "Log.Info";
                    break;
            }

            // Record the log as an event (truncated to keep buffer clean)
            string truncated = condition.Length > 200
                ? condition.Substring(0, 200) + "..."
                : condition;
            RuntimeDebugger.RecordEvent($"{eventName}:{truncated}");
        }
    }
}
