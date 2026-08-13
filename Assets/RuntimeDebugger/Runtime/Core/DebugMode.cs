namespace RuntimeDebugger
{
    public enum DebugMode
    {
        /// <summary>Lightweight ProfilerRecorder sampling, no deep trace.</summary>
        Performance,
        /// <summary>Manual high-detail trace for a fixed time window.</summary>
        DeepDebug,
        /// <summary>Lightweight ring buffer with automatic incident capture on trigger.</summary>
        TriggerDebug
    }
}
