using System.Collections.Generic;

namespace RuntimeDebugger
{
    /// <summary>
    /// Interface for systems that provide debuggable state.
    /// Register with StateRegistry to have state captured on snapshots.
    /// </summary>
    public interface IStateProvider
    {
        string Category { get; }

        /// <summary>
        /// Capture state snapshots into the provided list (or null for no-op).
        /// </summary>
        void Capture(List<StateSnapshot> outList, int frame, long timestampMs);
    }
}
