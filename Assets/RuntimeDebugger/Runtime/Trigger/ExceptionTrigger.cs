using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Triggers when a Unity exception is logged.
    /// Hooks into Application.logMessageReceived.
    /// </summary>
    public sealed class ExceptionTrigger : ITriggerCondition
    {
        public string Name => "Exception";
        public IncidentType IncidentType => IncidentType.Exception;

        private bool _triggered;
        private string _lastException;

        public ExceptionTrigger()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        ~ExceptionTrigger()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                _triggered = true;
                _lastException = condition;
            }
        }

        public bool Check()
        {
            if (_triggered)
            {
                _triggered = false;
                return true;
            }
            return false;
        }

        public string LastException => _lastException;
    }
}
