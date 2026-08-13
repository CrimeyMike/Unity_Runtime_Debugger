using System;

namespace RuntimeDebugger
{
    /// <summary>
    /// A node in the semantic trace tree. Stored flat in a ring buffer.
    /// Parent/child relationships are reconstructed from ParentId.
    /// </summary>
    [Serializable]
    public struct TraceNode
    {
        public int NodeId;
        public int ParentId;
        public int EventHash;
        public int Frame;
        public long StartMs;
        public long EndMs;
        public int ChildCount;

        public bool IsRoot => ParentId < 0;
        public bool IsFinished => EndMs > 0;
        public long DurationMs => IsFinished ? EndMs - StartMs : 0;

        public static TraceNode Create(int nodeId, int parentId, int eventHash, int frame, long startMs)
        {
            return new TraceNode
            {
                NodeId = nodeId,
                ParentId = parentId,
                EventHash = eventHash,
                Frame = frame,
                StartMs = startMs,
                EndMs = 0,
                ChildCount = 0
            };
        }
    }
}
