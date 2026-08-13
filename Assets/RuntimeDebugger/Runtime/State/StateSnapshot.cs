using System;

namespace RuntimeDebugger
{
    /// <summary>
    /// Snapshot of a single state value at a point in time.
    /// </summary>
    [Serializable]
    public struct StateSnapshot
    {
        public int Frame;
        public long TimestampMs;
        public int CategoryHash;
        public int KeyHash;
        public string Value;

        public static StateSnapshot Create(int frame, long timestampMs, string category, string key, string value)
        {
            return new StateSnapshot
            {
                Frame = frame,
                TimestampMs = timestampMs,
                CategoryHash = HashUtil.HashString(category),
                KeyHash = HashUtil.HashString(key),
                Value = value
            };
        }
    }
}
