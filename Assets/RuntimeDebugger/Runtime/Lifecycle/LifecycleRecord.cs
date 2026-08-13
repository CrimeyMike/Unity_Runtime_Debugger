using System;

namespace RuntimeDebugger
{
    public enum LifecyclePhase
    {
        Create,
        Enable,
        Disable,
        Destroy
    }

    /// <summary>
    /// A single lifecycle event record for a tracked object.
    /// </summary>
    [Serializable]
    public struct LifecycleRecord
    {
        public int ObjectId;
        public int TypeNameHash;
        public int Frame;
        public long TimestampMs;
        public int Phase;  // LifecyclePhase as int for serialization
        public int RelatedTaskId;

        public LifecyclePhase PhaseEnum => (LifecyclePhase)Phase;

        public static LifecycleRecord Create(int objectId, string typeName, int frame, long timestampMs, LifecyclePhase phase, int relatedTaskId = -1)
        {
            return new LifecycleRecord
            {
                ObjectId = objectId,
                TypeNameHash = HashUtil.HashString(typeName),
                Frame = frame,
                TimestampMs = timestampMs,
                Phase = (int)phase,
                RelatedTaskId = relatedTaskId
            };
        }
    }
}
