using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Base class for automatic lifecycle tracking.
    ///
    /// Instead of calling RuntimeDebugger.Lifecycle.Track/OnEnable/OnDisable/OnDestroy manually,
    /// just inherit from this class:
    ///
    /// <code>
    /// public class MyPanel : DebuggableBehaviour
    /// {
    ///     // OnEnable/OnDisable/OnDestroy are already tracked.
    ///     // Just write your normal code.
    /// }
    /// </code>
    ///
    /// If you need to override OnEnable etc., call base.OnEnable() first.
    /// </summary>
    public abstract class DebuggableBehaviour : MonoBehaviour
    {
        /// <summary>The debugger-assigned object ID for this instance.</summary>
        protected int DebugObjectId { get; private set; } = -1;

        /// <summary>Override to control whether this object is tracked. Default: true.</summary>
        protected virtual bool EnableLifecycleTracking => true;

        protected virtual void Awake()
        {
            if (EnableLifecycleTracking && RuntimeDebugger.IsEnabled && RuntimeDebugger.IsInitialized)
            {
                DebugObjectId = RuntimeDebugger.Lifecycle.Track(
                    GetType().Name,
                    Time.frameCount,
                    TimeUtil.NowMs());
            }
        }

        protected virtual void OnEnable()
        {
            if (DebugObjectId >= 0 && RuntimeDebugger.IsEnabled)
            {
                RuntimeDebugger.Lifecycle.OnEnable(
                    DebugObjectId, GetType().Name,
                    Time.frameCount, TimeUtil.NowMs());
            }
        }

        protected virtual void OnDisable()
        {
            if (DebugObjectId >= 0 && RuntimeDebugger.IsEnabled)
            {
                RuntimeDebugger.Lifecycle.OnDisable(
                    DebugObjectId, GetType().Name,
                    Time.frameCount, TimeUtil.NowMs());
            }
        }

        protected virtual void OnDestroy()
        {
            if (DebugObjectId >= 0 && RuntimeDebugger.IsEnabled)
            {
                RuntimeDebugger.Lifecycle.OnDestroy(
                    DebugObjectId, GetType().Name,
                    Time.frameCount, TimeUtil.NowMs());

                // Notify async tracker — any pending async tasks for this owner
                // will be marked as potential race conditions
                RuntimeDebugger.Async.NotifyOwnerDestroyed(
                    DebugObjectId, Time.frameCount, TimeUtil.NowMs());
            }
        }
    }
}
