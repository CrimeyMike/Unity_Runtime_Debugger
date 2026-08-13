using System;

namespace RuntimeDebugger
{
    /// <summary>
    /// A runtime event record. Uses int hash instead of string to avoid GC.
    /// </summary>
    [Serializable]
    public struct RuntimeEvent
    {
        public int Frame;
        public long TimestampMs;
        public int EventHash;
        public int ContextId;

        public static RuntimeEvent Create(int frame, long timestampMs, int eventHash, int contextId = -1)
        {
            return new RuntimeEvent
            {
                Frame = frame,
                TimestampMs = timestampMs,
                EventHash = eventHash,
                ContextId = contextId
            };
        }

        public bool IsValid => EventHash != 0;
    }
}
