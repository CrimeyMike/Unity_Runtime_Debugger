using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Tracks object lifecycle events (Create/Enable/Disable/Destroy).
    /// Detects async callbacks on destroyed objects (race conditions).
    /// </summary>
    public sealed class LifecycleTracker
    {
        private readonly RingBuffer<LifecycleRecord> _buffer;
        private readonly Dictionary<int, bool> _aliveObjects = new Dictionary<int, bool>();
        private int _nextId;

        public int TrackedCount => _aliveObjects.Count;
        public int RecordCount => _buffer.Count;
        public bool IsFrozen => _buffer.IsFrozen;

        public LifecycleTracker(int capacity)
        {
            _buffer = new RingBuffer<LifecycleRecord>(capacity);
            _nextId = 1;
        }

        /// <summary>
        /// Start tracking an object. Returns the assigned ObjectId.
        /// </summary>
        public int Track(string typeName, int frame, long timestampMs)
        {
            int id = _nextId++;
            _aliveObjects[id] = true;
            RuntimeDebugger.RegisterName(HashUtil.HashString(typeName), typeName);
            _buffer.Write(LifecycleRecord.Create(id, typeName, frame, timestampMs, LifecyclePhase.Create));
            return id;
        }

        public void OnEnable(int objectId, string typeName, int frame, long timestampMs)
        {
            if (!_aliveObjects.ContainsKey(objectId)) return;
            _buffer.Write(LifecycleRecord.Create(objectId, typeName, frame, timestampMs, LifecyclePhase.Enable));
        }

        public void OnDisable(int objectId, string typeName, int frame, long timestampMs)
        {
            if (!_aliveObjects.ContainsKey(objectId)) return;
            _buffer.Write(LifecycleRecord.Create(objectId, typeName, frame, timestampMs, LifecyclePhase.Disable));
        }

        public void OnDestroy(int objectId, string typeName, int frame, long timestampMs)
        {
            if (!_aliveObjects.ContainsKey(objectId)) return;
            _aliveObjects[objectId] = false;
            _buffer.Write(LifecycleRecord.Create(objectId, typeName, frame, timestampMs, LifecyclePhase.Destroy));
        }

        /// <summary>Check if an object is still alive (not destroyed).</summary>
        public bool IsAlive(int objectId)
        {
            return _aliveObjects.TryGetValue(objectId, out bool alive) && alive;
        }

        /// <summary>
        /// Mark that an async task is about to callback on an object.
        /// If the object is destroyed, this is a race condition.
        /// Returns true if a race condition was detected.
        /// </summary>
        public bool CheckAsyncCallback(int objectId, int taskId, string typeName, int frame, long timestampMs)
        {
            if (!_aliveObjects.TryGetValue(objectId, out bool alive) || !alive)
            {
                // Race condition: callback on destroyed/unknown object
                _buffer.Write(LifecycleRecord.Create(objectId, typeName, frame, timestampMs, LifecyclePhase.Destroy, taskId));
                return true;
            }
            return false;
        }

        public LifecycleRecord[] GetRecords()
        {
            return _buffer.GetAll();
        }

        public LifecycleRecord[] GetLastRecords(int count)
        {
            return _buffer.GetLast(count);
        }

        public void Freeze() => _buffer.Freeze();
        public void Unfreeze() => _buffer.Unfreeze();

        public void Clear()
        {
            _buffer.Clear();
            _aliveObjects.Clear();
            _nextId = 1;
        }
    }
}
