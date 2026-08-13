using System.Collections.Generic;

namespace RuntimeDebugger
{
    /// <summary>
    /// Maintains the semantic trace tree using an active-node stack and a ring buffer for completed nodes.
    /// Supports freeze for incident capture.
    /// </summary>
    public sealed class TraceTree
    {
        private readonly RingBuffer<TraceNode> _completed;
        private readonly TraceNode[] _activeStack;
        private int _activeDepth;
        private int _nodeIdCounter;

        public int CompletedCount => _completed.Count;
        public int ActiveDepth => _activeDepth;
        public int CurrentNodeId => _activeDepth > 0 ? _activeStack[_activeDepth - 1].NodeId : -1;
        public bool IsFrozen => _completed.IsFrozen;
        public int Capacity => _completed.Capacity;

        public TraceTree(int capacity, int maxDepth = 64)
        {
            _completed = new RingBuffer<TraceNode>(capacity);
            _activeStack = new TraceNode[maxDepth];
            _activeDepth = 0;
            _nodeIdCounter = 0;
        }

        /// <summary>
        /// Begin a trace node. Returns the assigned nodeId for use with EndTrace.
        /// </summary>
        public int BeginTrace(int eventHash, int frame, long startMs)
        {
            if (_activeDepth >= _activeStack.Length)
                return -1;

            int nodeId = _nodeIdCounter++;
            int parentId = _activeDepth > 0 ? _activeStack[_activeDepth - 1].NodeId : -1;

            _activeStack[_activeDepth] = TraceNode.Create(nodeId, parentId, eventHash, frame, startMs);
            _activeDepth++;
            return nodeId;
        }

        /// <summary>
        /// End a trace node. Writes the completed node to the ring buffer (unless frozen).
        /// </summary>
        public void EndTrace(int nodeId, long endMs)
        {
            if (_activeDepth == 0 || nodeId < 0)
                return;

            int topIndex = _activeDepth - 1;

            // Walk back to find the matching node (handles out-of-order disposal)
            int matchIndex = -1;
            for (int i = topIndex; i >= 0; i--)
            {
                if (_activeStack[i].NodeId == nodeId)
                {
                    matchIndex = i;
                    break;
                }
            }

            if (matchIndex < 0)
                return;

            // Complete the matched node
            var node = _activeStack[matchIndex];
            node.EndMs = endMs;

            // Update parent's child count
            if (matchIndex > 0)
            {
                var parent = _activeStack[matchIndex - 1];
                parent.ChildCount++;
                _activeStack[matchIndex - 1] = parent;
            }

            // Write to ring buffer
            _completed.Write(node);

            // Collapse: remove this node and everything above it from the stack
            _activeDepth = matchIndex;
        }

        /// <summary>Return all completed trace nodes (oldest first).</summary>
        public TraceNode[] GetTree()
        {
            return _completed.GetAll();
        }

        /// <summary>Return the last N completed nodes (most recent).</summary>
        public TraceNode[] GetLast(int count)
        {
            return _completed.GetLast(count);
        }

        public void Freeze() => _completed.Freeze();
        public void Unfreeze() => _completed.Unfreeze();

        public void Clear()
        {
            _completed.Clear();
            _activeDepth = 0;
            _nodeIdCounter = 0;
        }
    }
}
