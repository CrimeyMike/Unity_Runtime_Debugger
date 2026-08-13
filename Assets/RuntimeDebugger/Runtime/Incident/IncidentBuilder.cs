using System.Collections.Generic;

namespace RuntimeDebugger
{
    /// <summary>
    /// Assembles a RuntimeIncident from frozen runtime buffers.
    /// </summary>
    public static class IncidentBuilder
    {
        /// <summary>
        /// Build an incident from the current state of the RuntimeDebugger.
        /// Freezes buffers, extracts data, then unfreezes.
        /// </summary>
        public static RuntimeIncident Build(
            IncidentType type,
            string description,
            int triggerFrame,
            long triggerTimestampMs,
            TraceNode[] preTrace,
            TraceNode[] postTrace,
            RuntimeEvent[] events)
        {
            var incident = new RuntimeIncident
            {
                Type = type,
                TriggerFrame = triggerFrame,
                TriggerTimestampMs = triggerTimestampMs,
                TriggerDescription = description
            };

            if (preTrace != null)
                incident.PreTrace.AddRange(preTrace);

            if (postTrace != null)
                incident.PostTrace.AddRange(postTrace);

            if (events != null)
                incident.Events.AddRange(events);

            return incident;
        }

        /// <summary>
        /// Build an incident from the current RuntimeDebugger state.
        /// Freezes buffers, extracts all data as pre-trace, then unfreezes.
        /// </summary>
        public static RuntimeIncident BuildFromCurrentState(
            IncidentType type,
            string description)
        {
            RuntimeDebugger.FreezeBuffers();

            var preTrace = RuntimeDebugger.GetTraceTree();
            var events = RuntimeDebugger.GetEvents();
            var lifecycle = RuntimeDebugger.GetLifecycleRecords();
            var asyncRecords = RuntimeDebugger.GetAsyncRecords();
            var resourceRecords = RuntimeDebugger.GetResourceRecords();
            var stateSnapshots = RuntimeDebugger.GetStateSnapshots();
            var perfMetrics = RuntimeDebugger.GetPerfMetrics();

            var incident = Build(
                type,
                description,
                RuntimeDebugger.CurrentFrame,
                TimeUtil.NowMs(),
                preTrace,
                null,
                events);

            if (lifecycle != null)
                incident.LifecycleRecords.AddRange(lifecycle);
            if (asyncRecords != null)
                incident.AsyncRecords.AddRange(asyncRecords);
            if (resourceRecords != null)
                incident.ResourceRecords.AddRange(resourceRecords);
            if (stateSnapshots != null)
                incident.StateSnapshots.AddRange(stateSnapshots);
            if (perfMetrics != null)
                incident.Metrics.AddRange(perfMetrics);

            RuntimeDebugger.UnfreezeBuffers();

            return incident;
        }
    }
}
