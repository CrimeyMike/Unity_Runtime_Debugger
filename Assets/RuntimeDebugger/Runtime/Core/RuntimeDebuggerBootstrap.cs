using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Bootstraps the RuntimeDebugger on Awake and updates it every frame.
    /// Place in any scene that needs runtime debugging.
    /// </summary>
    public class RuntimeDebuggerBootstrap : MonoBehaviour
    {
        [SerializeField] private int _traceCapacity = 4096;
        [SerializeField] private int _eventCapacity = 4096;

        private void Awake()
        {
            if (!RuntimeDebugger.IsInitialized)
            {
                RuntimeDebugger.Initialize(_traceCapacity, _eventCapacity);
            }
        }

        private void Update()
        {
            if (RuntimeDebugger.IsInitialized)
            {
                RuntimeDebugger.OnFrameUpdate();
            }
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                RuntimeDebugger.OnException(new System.Exception(condition));
            }
        }
    }
}
