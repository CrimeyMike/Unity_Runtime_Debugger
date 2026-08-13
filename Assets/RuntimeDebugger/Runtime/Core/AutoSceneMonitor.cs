using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Zero-code automatic scene monitoring.
    ///
    /// Polls the scene every N frames and detects:
    /// - New MonoBehaviours created (via Instantiate / AddComponent)
    /// - Objects destroyed
    /// - Objects enabled / disabled
    ///
    /// NO game code modification required.
    /// Auto-installed by RuntimeDebugger.Initialize().
    /// </summary>
    public class AutoSceneMonitor : MonoBehaviour
    {
        private class TrackedInfo
        {
            public int ObjectId;
            public string TypeName;
            public bool WasActive;
        }

        private readonly Dictionary<int, TrackedInfo> _tracked = new Dictionary<int, TrackedInfo>();
        private readonly Dictionary<int, TrackedInfo> _pendingDestroy = new Dictionary<int, TrackedInfo>();
        private float _scanTimer;
        private float _scanInterval = 0.2f; // scan every 0.2s

        // Avoid GC: reuse lists
        private readonly List<int> _toRemove = new List<int>();
        private MonoBehaviour[] _scratchArray = System.Array.Empty<MonoBehaviour>();

        /// <summary>
        /// Auto-create the monitor GameObject. Called by RuntimeDebugger.Initialize().
        /// </summary>
        public static AutoSceneMonitor Install()
        {
            // Check if already exists
            var existing = FindObjectOfType<AutoSceneMonitor>();
            if (existing != null)
                return existing;

            var go = new GameObject("[RuntimeDebugger] AutoSceneMonitor");
            DontDestroyOnLoad(go);
            return go.AddComponent<AutoSceneMonitor>();
        }

        private void Update()
        {
            if (!RuntimeDebugger.IsEnabled || !RuntimeDebugger.IsInitialized) return;

            _scanTimer += Time.unscaledDeltaTime;
            if (_scanTimer < _scanInterval) return;
            _scanTimer = 0f;

            ScanScene();
        }

        private void ScanScene()
        {
            // Find all MonoBehaviours in the scene
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            if (behaviours == null) return;

            var currentInstanceIds = new HashSet<int>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null) continue;

                int instanceId = b.GetInstanceID();
                currentInstanceIds.Add(instanceId);

                bool isActive = b.gameObject.activeInHierarchy && b.enabled;

                if (!_tracked.TryGetValue(instanceId, out var info))
                {
                    // New object detected — auto-track it
                    int objId = RuntimeDebugger.Lifecycle.Track(
                        b.GetType().Name,
                        Time.frameCount,
                        TimeUtil.NowMs());

                    _tracked[instanceId] = new TrackedInfo
                    {
                        ObjectId = objId,
                        TypeName = b.GetType().Name,
                        WasActive = isActive
                    };

                    if (isActive)
                    {
                        RuntimeDebugger.Lifecycle.OnEnable(
                            objId, b.GetType().Name,
                            Time.frameCount, TimeUtil.NowMs());
                    }

                    // Also record as an event
                    RuntimeDebugger.RecordEvent($"Object.Create:{b.GetType().Name}");
                }
                else
                {
                    // Check for enable/disable state change
                    if (info.WasActive != isActive)
                    {
                        info.WasActive = isActive;
                        if (isActive)
                        {
                            RuntimeDebugger.Lifecycle.OnEnable(
                                info.ObjectId, info.TypeName,
                                Time.frameCount, TimeUtil.NowMs());
                            RuntimeDebugger.RecordEvent($"Object.Enable:{info.TypeName}");
                        }
                        else
                        {
                            RuntimeDebugger.Lifecycle.OnDisable(
                                info.ObjectId, info.TypeName,
                                Time.frameCount, TimeUtil.NowMs());
                            RuntimeDebugger.RecordEvent($"Object.Disable:{info.TypeName}");
                        }
                    }
                }
            }

            // Detect destroyed objects (in previous set but not in current)
            _toRemove.Clear();
            foreach (var kvp in _tracked)
            {
                if (!currentInstanceIds.Contains(kvp.Key))
                {
                    // Object was destroyed
                    RuntimeDebugger.Lifecycle.OnDestroy(
                        kvp.Value.ObjectId, kvp.Value.TypeName,
                        Time.frameCount, TimeUtil.NowMs());

                    // Notify async tracker — any pending async tasks for this owner
                    RuntimeDebugger.Async.NotifyOwnerDestroyed(
                        kvp.Value.ObjectId, Time.frameCount, TimeUtil.NowMs());

                    RuntimeDebugger.RecordEvent($"Object.Destroy:{kvp.Value.TypeName}");
                    _toRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _toRemove.Count; i++)
                _tracked.Remove(_toRemove[i]);
        }

        private void OnDestroy()
        {
            // Clean up all tracking on shutdown
            _tracked.Clear();
            _toRemove.Clear();
        }
    }
}
