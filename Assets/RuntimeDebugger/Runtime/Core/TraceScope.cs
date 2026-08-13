using System;
using Unity.Profiling;

namespace RuntimeDebugger
{
    /// <summary>
    /// Disposable scope for semantic tracing. Created by RuntimeDebugger.Trace().
    /// Also creates a ProfilerMarker for native profiler integration.
    /// When default-initialized (debugger disabled), Dispose is a no-op.
    /// </summary>
    public struct TraceScope : IDisposable
    {
        private int _nodeId;
        private bool _active;
        private ProfilerMarker.AutoScope _profilerScope;

        internal TraceScope(int nodeId, string name)
        {
            _nodeId = nodeId;
            _active = true;
            _profilerScope = new ProfilerMarker(name).Auto();
        }

        public void Dispose()
        {
            if (_active)
            {
                _profilerScope.Dispose();
                RuntimeDebugger.EndTrace(_nodeId);
                _active = false;
            }
        }
    }
}
