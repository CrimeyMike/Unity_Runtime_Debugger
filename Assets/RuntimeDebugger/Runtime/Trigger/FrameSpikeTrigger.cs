namespace RuntimeDebugger
{
    /// <summary>
    /// Triggers when frame time exceeds a threshold (default: 33.3ms = 30fps).
    /// </summary>
    public sealed class FrameSpikeTrigger : ITriggerCondition
    {
        public string Name => "FrameSpike";
        public IncidentType IncidentType => IncidentType.PerformanceSpike;

        private readonly double _thresholdMs;

        public FrameSpikeTrigger(double thresholdMs = 33.3)
        {
            _thresholdMs = thresholdMs;
        }

        public bool Check()
        {
            if (!RuntimeDebugger.IsEnabled || !RuntimeDebugger.IsInitialized)
                return false;

            double frameTime = RuntimeDebugger.Performance?.GetLatestValue(MetricIds.FrameTime) ?? 0;
            return frameTime > _thresholdMs;
        }

        public double ThresholdMs => _thresholdMs;
        public double CurrentFrameTime => RuntimeDebugger.Performance?.GetLatestValue(MetricIds.FrameTime) ?? 0;
    }
}
