namespace RuntimeDebugger
{
    /// <summary>
    /// Triggers when GC allocation per frame exceeds a threshold (default: 1MB).
    /// </summary>
    public sealed class GCSpikeTrigger : ITriggerCondition
    {
        public string Name => "GCSpike";
        public IncidentType IncidentType => IncidentType.GCSpike;

        private readonly double _thresholdBytes;

        public GCSpikeTrigger(double thresholdBytes = 1048576) // 1MB
        {
            _thresholdBytes = thresholdBytes;
        }

        public bool Check()
        {
            if (!RuntimeDebugger.IsEnabled || !RuntimeDebugger.IsInitialized)
                return false;

            double gcAlloc = RuntimeDebugger.Performance?.GetLatestValue(MetricIds.GCAlloc) ?? 0;
            return gcAlloc > _thresholdBytes;
        }

        public double ThresholdBytes => _thresholdBytes;
        public double CurrentGCAlloc => RuntimeDebugger.Performance?.GetLatestValue(MetricIds.GCAlloc) ?? 0;
    }
}
