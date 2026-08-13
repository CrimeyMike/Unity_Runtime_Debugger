using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Tracks async task lifecycle. Detects owner-destroyed-before-complete (race conditions).
    /// </summary>
    public sealed class AsyncTracker
    {
        private readonly RingBuffer<AsyncTraceRecord> _buffer;
        private readonly Dictionary<int, AsyncTraceRecord> _activeTasks = new Dictionary<int, AsyncTraceRecord>();
        private int _nextTaskId;

        public int ActiveTaskCount => _activeTasks.Count;
        public int CompletedCount => _buffer.Count;
        public bool IsFrozen => _buffer.IsFrozen;

        public AsyncTracker(int capacity)
        {
            _buffer = new RingBuffer<AsyncTraceRecord>(capacity);
            _nextTaskId = 1;
        }

        /// <summary>Start tracking an async task. Returns the assigned TaskId.</summary>
        public int StartTask(string operation, int ownerObjectId, int frame, long timestampMs)
        {
            int taskId = _nextTaskId++;
            var record = AsyncTraceRecord.Create(taskId, ownerObjectId, operation, frame, timestampMs);
            RuntimeDebugger.RegisterName(HashUtil.HashString(operation), operation);
            _activeTasks[taskId] = record;
            return taskId;
        }

        public void Complete(int taskId, int frame, long timestampMs)
        {
            if (!_activeTasks.TryGetValue(taskId, out var record))
                return;

            record.CompleteFrame = frame;
            record.CompleteMs = timestampMs;
            record.Status = (int)AsyncStatus.Completed;
            _buffer.Write(record);
            _activeTasks.Remove(taskId);
        }

        public void Cancel(int taskId, int frame, long timestampMs)
        {
            if (!_activeTasks.TryGetValue(taskId, out var record))
                return;

            record.CompleteFrame = frame;
            record.CompleteMs = timestampMs;
            record.Status = (int)AsyncStatus.Cancelled;
            _buffer.Write(record);
            _activeTasks.Remove(taskId);
        }

        public void Fail(int taskId, int frame, long timestampMs)
        {
            if (!_activeTasks.TryGetValue(taskId, out var record))
                return;

            record.CompleteFrame = frame;
            record.CompleteMs = timestampMs;
            record.Status = (int)AsyncStatus.Failed;
            _buffer.Write(record);
            _activeTasks.Remove(taskId);
        }

        /// <summary>
        /// Notify that an owner object was destroyed. Marks all active tasks for that owner.
        /// </summary>
        public void NotifyOwnerDestroyed(int ownerObjectId, int frame, long timestampMs)
        {
            // We can't modify dictionary during iteration, so collect keys first
            var keysToUpdate = new List<int>();
            foreach (var kvp in _activeTasks)
            {
                if (kvp.Value.OwnerObjectId == ownerObjectId)
                    keysToUpdate.Add(kvp.Key);
            }

            foreach (var key in keysToUpdate)
            {
                var record = _activeTasks[key];
                record.OwnerDestroyedFrame = frame;
                record.OwnerDestroyedMs = timestampMs;
                _activeTasks[key] = record;
            }
        }

        /// <summary>Get all tasks that have a race condition (owner destroyed before completion).</summary>
        public List<AsyncTraceRecord> GetRacingTasks()
        {
            var result = new List<AsyncTraceRecord>();
            var all = _buffer.GetAll();
            foreach (var r in all)
            {
                if (r.OwnerDestroyedBeforeComplete)
                    result.Add(r);
            }
            return result;
        }

        /// <summary>Check if a task is still active.</summary>
        public bool IsActive(int taskId)
        {
            return _activeTasks.ContainsKey(taskId);
        }

        public AsyncTraceRecord[] GetRecords()
        {
            return _buffer.GetAll();
        }

        public AsyncTraceRecord[] GetLastRecords(int count)
        {
            return _buffer.GetLast(count);
        }

        public void Freeze() => _buffer.Freeze();
        public void Unfreeze() => _buffer.Unfreeze();

        public void Clear()
        {
            _buffer.Clear();
            _activeTasks.Clear();
            _nextTaskId = 1;
        }
    }
}
