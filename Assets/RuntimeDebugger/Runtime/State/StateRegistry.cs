using System.Collections.Generic;

namespace RuntimeDebugger
{
    /// <summary>
    /// Registry of state providers. Call CaptureAll() to snapshot all registered providers.
    /// </summary>
    public sealed class StateRegistry
    {
        private readonly List<IStateProvider> _providers = new List<IStateProvider>();
        private readonly RingBuffer<StateSnapshot> _buffer;

        public int ProviderCount => _providers.Count;
        public int SnapshotCount => _buffer.Count;
        public bool IsFrozen => _buffer.IsFrozen;

        public StateRegistry(int capacity)
        {
            _buffer = new RingBuffer<StateSnapshot>(capacity);
        }

        public void Register(IStateProvider provider)
        {
            if (!_providers.Contains(provider))
                _providers.Add(provider);
        }

        public void Unregister(IStateProvider provider)
        {
            _providers.Remove(provider);
        }

        /// <summary>
        /// Capture state from all registered providers into the internal ring buffer.
        /// </summary>
        public void CaptureAll(int frame, long timestampMs)
        {
            for (int i = 0; i < _providers.Count; i++)
            {
                _providers[i].Capture(null, frame, timestampMs);
            }
        }

        /// <summary>
        /// Capture state from all registered providers into the provided list.
        /// Does NOT write to the internal ring buffer.
        /// </summary>
        public void CaptureAll(int frame, long timestampMs, List<StateSnapshot> outList)
        {
            for (int i = 0; i < _providers.Count; i++)
            {
                _providers[i].Capture(outList, frame, timestampMs);
            }
        }

        /// <summary>
        /// Write a state snapshot directly to the internal ring buffer.
        /// </summary>
        public void WriteSnapshot(StateSnapshot snapshot)
        {
            _buffer.Write(snapshot);
        }

        public StateSnapshot[] GetSnapshots()
        {
            return _buffer.GetAll();
        }

        public StateSnapshot[] GetLastSnapshots(int count)
        {
            return _buffer.GetLast(count);
        }

        public void Freeze() => _buffer.Freeze();
        public void Unfreeze() => _buffer.Unfreeze();
        public void Clear()
        {
            _buffer.Clear();
            _providers.Clear();
        }
    }
}
