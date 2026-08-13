using System;

namespace RuntimeDebugger
{
    /// <summary>
    /// Custom trigger that fires when a user-defined condition returns true.
    /// </summary>
    public sealed class CustomTrigger : ITriggerCondition
    {
        public string Name { get; }
        public IncidentType IncidentType { get; }

        private readonly Func<bool> _condition;

        public CustomTrigger(string name, Func<bool> condition, IncidentType incidentType = IncidentType.Custom)
        {
            Name = name;
            IncidentType = incidentType;
            _condition = condition;
        }

        public bool Check()
        {
            if (!RuntimeDebugger.IsEnabled || !RuntimeDebugger.IsInitialized)
                return false;
            return _condition?.Invoke() ?? false;
        }
    }
}
