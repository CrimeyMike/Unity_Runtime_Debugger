using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Manages a debug session lifecycle (mode, duration, frame tracking).
    /// Created by RuntimeDebugger.StartSession.
    /// </summary>
    public sealed class DebugSession
    {
        public DebugMode Mode { get; }
        public float DurationSec { get; }
        public long StartFrame { get; private set; }
        public long StartTimestampMs { get; private set; }
        public long EndFrame { get; private set; }
        public bool IsActive { get; private set; }

        public bool IsExpired
        {
            get
            {
                if (!IsActive || DurationSec <= 0)
                    return false;
                return TimeUtil.ElapsedMs(StartTimestampMs) >= (long)(DurationSec * 1000);
            }
        }

        public DebugSession(DebugMode mode, float durationSec = 0f)
        {
            Mode = mode;
            DurationSec = durationSec;
            IsActive = false;
        }

        public void Start()
        {
            StartFrame = Time.frameCount;
            StartTimestampMs = TimeUtil.NowMs();
            IsActive = true;
        }

        public void Stop()
        {
            if (!IsActive) return;
            EndFrame = Time.frameCount;
            IsActive = false;
        }

        public long ElapsedFrames => Time.frameCount - StartFrame;
        public long ElapsedMs => TimeUtil.ElapsedMs(StartTimestampMs);

        internal void OnFrameUpdate()
        {
            // Hook for per-frame session logic (e.g., auto-stop, trigger checks)
            // Will be extended in M3 (Performance) and M4 (Trigger)
        }
    }
}
