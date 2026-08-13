namespace RuntimeDebugger
{
    /// <summary>
    /// Interface for trigger conditions that detect anomalies and initiate incident capture.
    /// </summary>
    public interface ITriggerCondition
    {
        string Name { get; }
        IncidentType IncidentType { get; }

        /// <summary>
        /// Check if the trigger condition is met.
        /// Called every frame by TriggerSystem.
        /// </summary>
        bool Check();
    }
}
