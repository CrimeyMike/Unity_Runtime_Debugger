using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// State machine for automatic incident detection and capture.
    ///
    /// Flow:
    ///   Idle → (Trigger detected) → Freezing → DeepCapture → IncidentReady → Idle
    ///
    /// On trigger:
    ///   1. Freeze all ring buffers (preserves pre-incident data)
    ///   2. Start Deep Capture (high-frequency sampling for M seconds)
    ///   3. Build incident from frozen + new data
    ///   4. Return to normal operation
    /// </summary>
    public sealed class TriggerSystem
    {
        public enum State
        {
            Idle,
            Freezing,
            DeepCapture,
            IncidentReady
        }

        private readonly List<ITriggerCondition> _triggers = new List<ITriggerCondition>();
        private readonly float _deepCaptureDurationSec;
        private readonly int _deepCaptureFrames;

        private State _state = State.Idle;
        private long _captureStartMs;
        private int _captureStartFrame;
        private RuntimeIncident _pendingIncident;
        private ITriggerCondition _firedTrigger;

        public State CurrentState => _state;
        public bool HasPendingIncident => _state == State.IncidentReady && _pendingIncident != null;
        public RuntimeIncident PendingIncident => _pendingIncident;
        public int TriggerCount => _triggers.Count;

        public TriggerSystem(float deepCaptureDurationSec = 2f, int deepCaptureFrames = 120)
        {
            _deepCaptureDurationSec = deepCaptureDurationSec;
            _deepCaptureFrames = deepCaptureFrames;
        }

        public void RegisterTrigger(ITriggerCondition trigger)
        {
            if (!_triggers.Contains(trigger))
                _triggers.Add(trigger);
        }

        public void UnregisterTrigger(ITriggerCondition trigger)
        {
            _triggers.Remove(trigger);
        }

        /// <summary>Manually trigger an incident.</summary>
        public void TriggerManually(IncidentType type, string description)
        {
            if (_state != State.Idle) return;
            StartCapture(type, description, "Manual");
        }

        /// <summary>
        /// Called every frame by RuntimeDebugger. Checks triggers and manages state transitions.
        /// </summary>
        internal void OnFrameUpdate(int frame, long timestampMs)
        {
            switch (_state)
            {
                case State.Idle:
                    CheckTriggers(frame, timestampMs);
                    break;

                case State.Freezing:
                    // Freeze is instantaneous — transition to DeepCapture immediately
                    EnterDeepCapture(frame, timestampMs);
                    break;

                case State.DeepCapture:
                    // Check if capture duration has elapsed
                    bool timeExpired = TimeUtil.ElapsedMs(_captureStartMs) >= (long)(_deepCaptureDurationSec * 1000);
                    bool framesExpired = (frame - _captureStartFrame) >= _deepCaptureFrames;

                    if (timeExpired || framesExpired)
                    {
                        CompleteCapture(frame, timestampMs);
                    }
                    break;

                case State.IncidentReady:
                    // Wait for consumer to retrieve the incident
                    break;
            }
        }

        private void CheckTriggers(int frame, long timestampMs)
        {
            for (int i = 0; i < _triggers.Count; i++)
            {
                var trigger = _triggers[i];
                if (trigger.Check())
                {
                    StartCapture(trigger.IncidentType, $"{trigger.Name} trigger fired", trigger.Name);
                    _firedTrigger = trigger;
                    return;
                }
            }
        }

        private void StartCapture(IncidentType type, string description, string triggerName)
        {
            _state = State.Freezing;

            // Freeze all buffers to preserve pre-incident data
            RuntimeDebugger.FreezeBuffers();

            // Capture pre-incident data snapshot
            var preTrace = RuntimeDebugger.GetTraceTree();
            var events = RuntimeDebugger.GetEvents();
            var lifecycle = RuntimeDebugger.GetLifecycleRecords();
            var asyncRecords = RuntimeDebugger.GetAsyncRecords();
            var resourceRecords = RuntimeDebugger.GetResourceRecords();
            var stateSnapshots = RuntimeDebugger.GetStateSnapshots();
            var perfMetrics = RuntimeDebugger.GetPerfMetrics();

            _pendingIncident = new RuntimeIncident
            {
                Type = type,
                TriggerFrame = RuntimeDebugger.CurrentFrame,
                TriggerTimestampMs = TimeUtil.NowMs(),
                TriggerDescription = description
            };
            _pendingIncident.PreTrace.AddRange(preTrace);
            _pendingIncident.Events.AddRange(events);
            _pendingIncident.LifecycleRecords.AddRange(lifecycle);
            _pendingIncident.AsyncRecords.AddRange(asyncRecords);
            _pendingIncident.ResourceRecords.AddRange(resourceRecords);
            _pendingIncident.StateSnapshots.AddRange(stateSnapshots);
            _pendingIncident.Metrics.AddRange(perfMetrics);

            // Unfreeze to allow continued recording during DeepCapture
            RuntimeDebugger.UnfreezeBuffers();

            Debug.Log($"[TriggerSystem] Trigger fired: {triggerName} → entering Deep Capture");
        }

        private void EnterDeepCapture(int frame, long timestampMs)
        {
            _state = State.DeepCapture;
            _captureStartMs = timestampMs;
            _captureStartFrame = frame;
        }

        private void CompleteCapture(int frame, long timestampMs)
        {
            if (_pendingIncident == null)
            {
                _state = State.Idle;
                return;
            }

            // Collect post-incident data
            var postTrace = RuntimeDebugger.GetTraceTree();
            var postEvents = RuntimeDebugger.GetEvents();
            var postLifecycle = RuntimeDebugger.GetLifecycleRecords();
            var postAsync = RuntimeDebugger.GetAsyncRecords();
            var postResources = RuntimeDebugger.GetResourceRecords();
            var postMetrics = RuntimeDebugger.GetPerfMetrics();

            // Only add items that are newer than the trigger
            foreach (var t in postTrace)
                if (t.StartMs >= _pendingIncident.TriggerTimestampMs)
                    _pendingIncident.PostTrace.Add(t);

            foreach (var e in postEvents)
                if (e.TimestampMs >= _pendingIncident.TriggerTimestampMs)
                    _pendingIncident.Events.Add(e);

            foreach (var l in postLifecycle)
                if (l.TimestampMs >= _pendingIncident.TriggerTimestampMs)
                    _pendingIncident.LifecycleRecords.Add(l);

            foreach (var a in postAsync)
                if (a.StartMs >= _pendingIncident.TriggerTimestampMs)
                    _pendingIncident.AsyncRecords.Add(a);

            foreach (var r in postResources)
                if (r.TimestampMs >= _pendingIncident.TriggerTimestampMs)
                    _pendingIncident.ResourceRecords.Add(r);

            foreach (var m in postMetrics)
                if (m.TimestampMs >= _pendingIncident.TriggerTimestampMs)
                    _pendingIncident.Metrics.Add(m);

            _state = State.IncidentReady;
            Debug.Log($"[TriggerSystem] Deep Capture complete. Incident ready: {_pendingIncident.Type}");
        }

        /// <summary>Retrieve the pending incident and return to Idle.</summary>
        public RuntimeIncident RetrieveIncident()
        {
            var incident = _pendingIncident;
            _pendingIncident = null;
            _firedTrigger = null;
            _state = State.Idle;
            return incident;
        }

        public void Reset()
        {
            _pendingIncident = null;
            _firedTrigger = null;
            _state = State.Idle;
        }
    }
}
