using System;

namespace RuntimeDebugger
{
    public enum ResourceOperation
    {
        LoadStart,
        LoadComplete,
        LoadFail,
        Release
    }

    /// <summary>
    /// Record of a single resource operation (load/release/etc).
    /// </summary>
    [Serializable]
    public struct ResourceRecord
    {
        public int Frame;
        public long TimestampMs;
        public int AssetPathHash;
        public int OwnerObjectId;
        public int TaskId;
        public int Operation;  // ResourceOperation as int

        public ResourceOperation OperationEnum => (ResourceOperation)Operation;

        public static ResourceRecord Create(int frame, long timestampMs, string assetPath, int ownerObjectId, int taskId, ResourceOperation op)
        {
            return new ResourceRecord
            {
                Frame = frame,
                TimestampMs = timestampMs,
                AssetPathHash = HashUtil.HashString(assetPath),
                OwnerObjectId = ownerObjectId,
                TaskId = taskId,
                Operation = (int)op
            };
        }
    }
}
