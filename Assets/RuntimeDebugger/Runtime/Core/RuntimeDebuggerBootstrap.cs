using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeDebugger
{
    /// <summary>
    /// Auto-installing bootstrap. Call RuntimeDebuggerBootstrap.EnsureExists() to
    /// auto-create a hidden DontDestroyOnLoad GameObject.
    ///
    /// This is called automatically by [RuntimeInitializeOnLoadMethod] —
    /// the user does NOT need to place this in any scene.
    /// </summary>
    public class RuntimeDebuggerBootstrap : MonoBehaviour
    {
        [SerializeField] private int _traceCapacity = 4096;
        [SerializeField] private int _eventCapacity = 4096;

        /// <summary>
        /// Auto-initialize on game start — NO manual setup needed.
        /// This runs before any scene loads.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (!RuntimeDebugger.IsInitialized)
            {
                RuntimeDebugger.Initialize();

                // Auto-install the bootstrap updater
                var go = new GameObject("[RuntimeDebugger] Bootstrap");
                DontDestroyOnLoad(go);
                go.AddComponent<RuntimeDebuggerBootstrap>();

                // AutoSceneMonitor is installed by Initialize() when Application.isPlaying
            }
        }

        private void Update()
        {
            if (RuntimeDebugger.IsInitialized)
            {
                RuntimeDebugger.OnFrameUpdate();
            }
        }
    }
}
