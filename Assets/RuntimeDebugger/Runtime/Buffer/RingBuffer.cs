using System;

namespace RuntimeDebugger
{
    /// <summary>
    /// Generic ring buffer with fixed capacity, O(1) write/overwrite, and freeze support.
    /// Pre-allocates its backing array — zero GC after construction.
    /// </summary>
    public sealed class RingBuffer<T>
    {
        private readonly T[] _data;
        private int _head;
        private int _count;
        private bool _frozen;

        public int Capacity => _data.Length;
        public int Count => _count;
        public bool IsFrozen => _frozen;

        public RingBuffer(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("Capacity must be >= 1", nameof(capacity));
            _data = new T[capacity];
            _head = 0;
            _count = 0;
            _frozen = false;
        }

        /// <summary>
        /// Write an item. Overwrites the oldest data when full. No-op if frozen.
        /// </summary>
        public void Write(T item)
        {
            if (_frozen) return;
            _data[_head] = item;
            _head = (_head + 1) % _data.Length;
            if (_count < _data.Length)
                _count++;
        }

        public void Freeze() => _frozen = true;
        public void Unfreeze() => _frozen = false;

        /// <summary>
        /// Returns all valid items in chronological order (oldest first).
        /// Allocates a new array.
        /// </summary>
        public T[] GetAll()
        {
            var result = new T[_count];
            int start = (_head - _count + _data.Length) % _data.Length;
            for (int i = 0; i < _count; i++)
            {
                result[i] = _data[(start + i) % _data.Length];
            }
            return result;
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> items, starting from the oldest valid item.
        /// </summary>
        public T[] GetRange(int startIndex, int count)
        {
            if (startIndex < 0 || startIndex >= _count)
                return Array.Empty<T>();
            int actual = Math.Min(count, _count - startIndex);
            var result = new T[actual];
            int start = (_head - _count + _data.Length) % _data.Length;
            for (int i = 0; i < actual; i++)
            {
                result[i] = _data[(start + startIndex + i) % _data.Length];
            }
            return result;
        }

        /// <summary>
        /// Returns the last <paramref name="count"/> items (most recent N).
        /// </summary>
        public T[] GetLast(int count)
        {
            int actual = Math.Min(count, _count);
            var result = new T[actual];
            int start = (_head - actual + _data.Length) % _data.Length;
            for (int i = 0; i < actual; i++)
            {
                result[i] = _data[(start + i) % _data.Length];
            }
            return result;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            _frozen = false;
            Array.Clear(_data, 0, _data.Length);
        }
    }
}
