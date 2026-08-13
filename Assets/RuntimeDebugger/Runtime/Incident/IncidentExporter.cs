using System;
using System.IO;
using UnityEngine;

namespace RuntimeDebugger
{
    /// <summary>
    /// Exports a RuntimeIncident to a structured JSON bundle on disk.
    /// Output structure:
    ///   Incident_{timestamp}/
    ///   ├── incident.json
    ///   ├── timeline.json
    ///   ├── events.json
    ///   ├── lifecycle.json
    ///   ├── async.json
    ///   ├── resources.json
    ///   ├── states.json
    ///   └── metadata.json
    /// </summary>
    public static class IncidentExporter
    {
        [Serializable]
        private class TraceNodeArray { public TraceNode[] items; }

        [Serializable]
        private class RuntimeEventArray { public RuntimeEvent[] items; }

        [Serializable]
        private class LifecycleRecordArray { public LifecycleRecord[] items; }

        [Serializable]
        private class AsyncTraceRecordArray { public AsyncTraceRecord[] items; }

        [Serializable]
        private class ResourceRecordArray { public ResourceRecord[] items; }

        [Serializable]
        private class StateSnapshotArray { public StateSnapshot[] items; }

        [Serializable]
        private class PerfMetricArray { public PerfMetric[] items; }

        public static string Export(RuntimeIncident incident, string basePath = null)
        {
            if (incident == null)
                throw new ArgumentNullException(nameof(incident));

            if (string.IsNullOrEmpty(basePath))
                basePath = Path.Combine(Application.persistentDataPath, "Incidents");

            string dirName = $"Incident_{incident.Type}_{incident.TriggerTimestampMs}";
            string dirPath = Path.Combine(basePath, dirName);
            Directory.CreateDirectory(dirPath);

            // incident.json — summary
            var summary = incident.ToSummary();
            WriteJson(Path.Combine(dirPath, "incident.json"), JsonUtility.ToJson(summary, true));

            // timeline.json — pre + post trace nodes
            var allTraces = new System.Collections.Generic.List<TraceNode>();
            allTraces.AddRange(incident.PreTrace);
            allTraces.AddRange(incident.PostTrace);
            WriteJson(Path.Combine(dirPath, "timeline.json"),
                JsonUtility.ToJson(new TraceNodeArray { items = allTraces.ToArray() }, true));

            // events.json
            WriteJson(Path.Combine(dirPath, "events.json"),
                JsonUtility.ToJson(new RuntimeEventArray { items = incident.Events.ToArray() }, true));

            // lifecycle.json
            WriteJson(Path.Combine(dirPath, "lifecycle.json"),
                JsonUtility.ToJson(new LifecycleRecordArray { items = incident.LifecycleRecords.ToArray() }, true));

            // async.json
            WriteJson(Path.Combine(dirPath, "async.json"),
                JsonUtility.ToJson(new AsyncTraceRecordArray { items = incident.AsyncRecords.ToArray() }, true));

            // resources.json
            WriteJson(Path.Combine(dirPath, "resources.json"),
                JsonUtility.ToJson(new ResourceRecordArray { items = incident.ResourceRecords.ToArray() }, true));

            // states.json
            WriteJson(Path.Combine(dirPath, "states.json"),
                JsonUtility.ToJson(new StateSnapshotArray { items = incident.StateSnapshots.ToArray() }, true));

            // metrics.json
            WriteJson(Path.Combine(dirPath, "metrics.json"),
                JsonUtility.ToJson(new PerfMetricArray { items = incident.Metrics.ToArray() }, true));

            // metadata.json
            var metadata = new IncidentMetadata
            {
                UnityVersion = Application.unityVersion,
                ScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                ExportTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DebuggerMode = RuntimeDebugger.Mode.ToString(),
                TotalTraceNodes = allTraces.Count,
                TotalEvents = incident.Events.Count,
                TotalLifecycleRecords = incident.LifecycleRecords.Count,
                TotalAsyncRecords = incident.AsyncRecords.Count,
                TotalResourceRecords = incident.ResourceRecords.Count,
                TotalStateSnapshots = incident.StateSnapshots.Count,
                TotalMetrics = incident.Metrics.Count
            };
            WriteJson(Path.Combine(dirPath, "metadata.json"), JsonUtility.ToJson(metadata, true));

            Debug.Log($"[RuntimeDebugger] Incident exported to: {dirPath}");
            return dirPath;
        }

        private static void WriteJson(string path, string json)
        {
            File.WriteAllText(path, json);
        }
    }
}
