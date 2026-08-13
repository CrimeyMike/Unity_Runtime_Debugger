using System;

namespace RuntimeDebugger
{
    public enum AsyncStatus
    {
        Running,
        Completed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Record of an async task's lifecycle.
    /// </summary>
    [Serializable]
    public struct AsyncTraceRecord
    {
        public int TaskId;
        public int OwnerObjectId;
        public int OperationHash;
        public int StartFrame;
        public long StartMs;
        public int CompleteFrame;
        public long CompleteMs;
        public int Status;  // AsyncStatus as int
        public int OwnerDestroyedFrame;  // -1 = owner still alive
        public long OwnerDestroyedMs;

        public AsyncStatus StatusEnum => (AsyncStatus)Status;
        public bool OwnerDestroyedBeforeComplete => OwnerDestroyedFrame >= 0 && CompleteFrame >= 0;

        public static AsyncTraceRecord Create(int taskId, int ownerObjectId, string operation, int startFrame, long startMs)
        {
            return new AsyncTraceRecord
            {
                TaskId = taskId,
                OwnerObjectId = ownerObjectId,
                OperationHash = HashUtil.HashString(operation),
                StartFrame = startFrame,
                StartMs = startMs,
                CompleteFrame = -1,
                CompleteMs = 0,
                Status = (int)AsyncStatus.Running,
                OwnerDestroyedFrame = -1,
                OwnerDestroyedMs = 0
            };
        }
    }
}
