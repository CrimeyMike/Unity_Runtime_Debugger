using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Test behaviour that simulates two scenarios:
    /// 1. Turn.End → Event.Resolve → Technology.Unlock → UI.Refresh pipeline
    /// 2. Async lifecycle race: Panel created → async task started → panel destroyed → task completes
    /// 3. Resource duplicate load
    ///
    /// After N frames, captures an incident and exports it to JSON.
    /// </summary>
    public class DebugTestBehaviour : MonoBehaviour
    {
        [SerializeField] private int _captureAtFrame = 90;
        [SerializeField] private int _startTurnAtFrame = 30;
        [SerializeField] private int _startRaceAtFrame = 50;

        private bool _captured;

        private void Update()
        {
            if (Time.frameCount == _startTurnAtFrame)
            {
                SimulateTurnEnd();
            }

            if (Time.frameCount == _startRaceAtFrame)
            {
                SimulateLifecycleRace();
            }

            if (Time.frameCount >= _captureAtFrame && !_captured)
            {
                _captured = true;
                CaptureAndExport();
            }
        }

        private void SimulateTurnEnd()
        {
            // Start a Deep Debug session to enable performance sampling
            if (!RuntimeDebugger.IsEnabled) return;

            RuntimeDebugger.StartSession(DebugMode.DeepDebug, 10f);

            Debug.Log($"[DebugTest] Frame {Time.frameCount}: Simulating Turn.End pipeline");

            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");

                using (RuntimeDebugger.Trace("Event.Resolve"))
                {
                    RuntimeDebugger.RecordEvent("Event.Resolve");

                    using (RuntimeDebugger.Trace("Technology.Unlock"))
                    {
                        RuntimeDebugger.RecordEvent("Technology.Unlock");
                    }

                    using (RuntimeDebugger.Trace("UI.Refresh"))
                    {
                        RuntimeDebugger.RecordEvent("UI.Refresh");
                    }
                }
            }

            Debug.Log("[DebugTest] Turn.End pipeline complete.");
        }

        private void SimulateLifecycleRace()
        {
            Debug.Log($"[DebugTest] Frame {Time.frameCount}: Simulating lifecycle race condition");

            // Track a panel object
            int panelId = RuntimeDebugger.Lifecycle.Track("OperatorPanel", Time.frameCount, TimeUtil.NowMs());
            RuntimeDebugger.Lifecycle.OnEnable(panelId, "OperatorPanel", Time.frameCount, TimeUtil.NowMs());

            // Start an async resource load owned by the panel
            int taskId = RuntimeDebugger.Async.StartTask(
                "Addressables.LoadAssetAsync<Sprite>",
                panelId,
                Time.frameCount,
                TimeUtil.NowMs());

            // Record the resource load
            RuntimeDebugger.Resource.RecordLoadStart(
                "Assets/Sprites/TechIcon.png",
                panelId,
                Time.frameCount,
                TimeUtil.NowMs());

            // Panel gets disabled and destroyed before async completes
            RuntimeDebugger.Lifecycle.OnDisable(panelId, "OperatorPanel", Time.frameCount + 2, TimeUtil.NowMs());
            RuntimeDebugger.Lifecycle.OnDestroy(panelId, "OperatorPanel", Time.frameCount + 3, TimeUtil.NowMs());

            // Notify async tracker that owner was destroyed
            RuntimeDebugger.Async.NotifyOwnerDestroyed(panelId, Time.frameCount + 3, TimeUtil.NowMs());

            // Async task completes after owner destroyed → race condition!
            RuntimeDebugger.Async.Complete(taskId, Time.frameCount + 5, TimeUtil.NowMs());
            RuntimeDebugger.Resource.RecordLoadComplete(
                taskId,
                "Assets/Sprites/TechIcon.png",
                Time.frameCount + 5,
                TimeUtil.NowMs());

            // Check for race condition
            var racingTasks = RuntimeDebugger.Async.GetRacingTasks();
            if (racingTasks.Count > 0)
            {
                Debug.LogWarning($"[DebugTest] RACE CONDITION DETECTED: {racingTasks.Count} async task(s) completed after owner destroyed!");
            }

            // Also simulate duplicate resource load
            RuntimeDebugger.Resource.RecordLoadStart("Assets/Sprites/Duplicate.png", -1, Time.frameCount, TimeUtil.NowMs());
            RuntimeDebugger.Resource.RecordLoadStart("Assets/Sprites/Duplicate.png", -1, Time.frameCount, TimeUtil.NowMs());

            var duplicates = RuntimeDebugger.Resource.GetDuplicateLoads(10);
            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"[DebugTest] DUPLICATE LOAD DETECTED: {duplicates.Count} duplicate resource load(s)!");
            }
        }

        private void CaptureAndExport()
        {
            Debug.Log($"[DebugTest] Frame {Time.frameCount}: Capturing incident...");

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.AsyncFailure,
                "Lifecycle race condition + duplicate resource load");

            string exportPath = IncidentExporter.Export(incident);

            Debug.Log($"[DebugTest] Incident exported to: {exportPath}");
            Debug.Log($"[DebugTest] Trace: {incident.PreTrace.Count}, Events: {incident.Events.Count}, " +
                      $"Lifecycle: {incident.LifecycleRecords.Count}, Async: {incident.AsyncRecords.Count}, " +
                      $"Resources: {incident.ResourceRecords.Count}, States: {incident.StateSnapshots.Count}, " +
                      $"Metrics: {incident.Metrics.Count}");
        }
    }
}
