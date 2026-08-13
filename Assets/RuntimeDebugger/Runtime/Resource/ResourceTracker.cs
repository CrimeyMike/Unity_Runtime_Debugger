using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Tracks resource load/release operations. Detects duplicate asset loading.
    /// </summary>
    public sealed class ResourceTracker
    {
        private readonly RingBuffer<ResourceRecord> _buffer;
        private readonly Dictionary<int, int> _pendingLoads = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _activeHandles = new Dictionary<int, int>();

        public int PendingLoadCount => _pendingLoads.Count;
        public int ActiveHandleCount => _activeHandles.Count;
        public int RecordCount => _buffer.Count;
        public bool IsFrozen => _buffer.IsFrozen;

        public ResourceTracker(int capacity)
        {
            _buffer = new RingBuffer<ResourceRecord>(capacity);
        }

        /// <summary>Record the start of a resource load. Returns the assigned TaskId (-1 if duplicate detected).</summary>
        public int RecordLoadStart(string assetPath, int ownerObjectId, int frame, long timestampMs)
        {
            int pathHash = HashUtil.HashString(assetPath);
            RuntimeDebugger.RegisterName(pathHash, assetPath);

            // Check for duplicate load
            if (_pendingLoads.TryGetValue(pathHash, out int existingTaskId))
            {
                // Duplicate load! Record it but still return a new task id
                int newTaskId = _pendingLoads.Count + 1; // simplified
                _buffer.Write(ResourceRecord.Create(frame, timestampMs, assetPath, ownerObjectId, newTaskId, ResourceOperation.LoadStart));
                return newTaskId;
            }

            int taskId = _pendingLoads.Count + 1;
            _pendingLoads[pathHash] = taskId;
            _buffer.Write(ResourceRecord.Create(frame, timestampMs, assetPath, ownerObjectId, taskId, ResourceOperation.LoadStart));
            return taskId;
        }

        public void RecordLoadComplete(int taskId, string assetPath, int frame, long timestampMs)
        {
            int pathHash = HashUtil.HashString(assetPath);
            _pendingLoads.Remove(pathHash);
            _activeHandles[pathHash] = _activeHandles.TryGetValue(pathHash, out int c) ? c + 1 : 1;
            _buffer.Write(ResourceRecord.Create(frame, timestampMs, assetPath, -1, taskId, ResourceOperation.LoadComplete));
        }

        public void RecordLoadFail(int taskId, string assetPath, int frame, long timestampMs)
        {
            int pathHash = HashUtil.HashString(assetPath);
            _pendingLoads.Remove(pathHash);
            _buffer.Write(ResourceRecord.Create(frame, timestampMs, assetPath, -1, taskId, ResourceOperation.LoadFail));
        }

        public void RecordRelease(string assetPath, int frame, long timestampMs)
        {
            int pathHash = HashUtil.HashString(assetPath);
            if (_activeHandles.TryGetValue(pathHash, out int count))
            {
                if (count <= 1)
                    _activeHandles.Remove(pathHash);
                else
                    _activeHandles[pathHash] = count - 1;
            }
            _buffer.Write(ResourceRecord.Create(frame, timestampMs, assetPath, -1, -1, ResourceOperation.Release));
        }

        /// <summary>Check if an asset is currently being loaded (duplicate detection helper).</summary>
        public bool IsLoading(string assetPath)
        {
            return _pendingLoads.ContainsKey(HashUtil.HashString(assetPath));
        }

        /// <summary>Detect duplicate loads of the same asset within the last N records.</summary>
        public List<ResourceRecord> GetDuplicateLoads(int lookbackCount)
        {
            var records = _buffer.GetLast(lookbackCount);
            var seen = new Dictionary<int, int>();
            var duplicates = new List<ResourceRecord>();

            foreach (var r in records)
            {
                if (r.OperationEnum == ResourceOperation.LoadStart)
                {
                    if (seen.ContainsKey(r.AssetPathHash))
                        duplicates.Add(r);
                    else
                        seen[r.AssetPathHash] = 1;
                }
            }
            return duplicates;
        }

        public ResourceRecord[] GetRecords()
        {
            return _buffer.GetAll();
        }

        public ResourceRecord[] GetLastRecords(int count)
        {
            return _buffer.GetLast(count);
        }

        public void Freeze() => _buffer.Freeze();
        public void Unfreeze() => _buffer.Unfreeze();

        public void Clear()
        {
            _buffer.Clear();
            _pendingLoads.Clear();
            _activeHandles.Clear();
        }
    }
}
