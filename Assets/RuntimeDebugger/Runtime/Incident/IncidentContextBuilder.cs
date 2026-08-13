using System;
using System.Collections.Generic;
using System.Text;

namespace RuntimeDebugger
{
    /// <summary>
    /// Builds a structured context string from a RuntimeIncident for AI analysis.
    /// Extracts the most relevant trace timeline, metrics, lifecycle, and async data.
    /// </summary>
    public static class IncidentContextBuilder
    {
        /// <summary>
        /// Build a human-readable context from an incident.
        /// Controls output size to fit LLM context windows.
        /// </summary>
        public static string Build(RuntimeIncident incident, int maxTraceNodes = 50, int maxEvents = 50)
        {
            if (incident == null)
                return "No incident data.";

            var sb = new StringBuilder(4096);

            // ── Incident Summary ───────────────────────────────
            sb.AppendLine(DebugLocale.Get("section.incident"));
            sb.AppendLine($"Type: {incident.Type}");
            sb.AppendLine($"TriggerFrame: {incident.TriggerFrame}");
            sb.AppendLine($"TriggerTimestamp: {incident.TriggerTimestampMs}ms");
            sb.AppendLine($"Description: {incident.TriggerDescription}");
            sb.AppendLine();

            // ── Timeline ──────────────────────────────────────
            sb.AppendLine(DebugLocale.Get("section.timeline"));
            var allTraces = new List<TraceNode>();
            allTraces.AddRange(incident.PreTrace);
            allTraces.AddRange(incident.PostTrace);

            int traceCount = Math.Min(allTraces.Count, maxTraceNodes);
            for (int i = 0; i < traceCount; i++)
            {
                var node = allTraces[i];
                string name = RuntimeDebugger.GetEventName(node.EventHash);
                string indent = new string(' ', node.IsRoot ? 0 : 2);
                string duration = node.IsFinished ? $"{node.DurationMs}ms" : "active";
                sb.AppendLine($"{indent}{name} [Frame {node.Frame}] ({duration})");
            }
            if (allTraces.Count > maxTraceNodes)
                sb.AppendLine($"... ({allTraces.Count - maxTraceNodes} more trace nodes)");
            sb.AppendLine();

            // ── Metrics ───────────────────────────────────────
            if (incident.Metrics.Count > 0)
            {
                sb.AppendLine(DebugLocale.Get("section.metrics"));
                var latestByMetric = new Dictionary<int, PerfMetric>();
                foreach (var m in incident.Metrics)
                {
                    if (!latestByMetric.ContainsKey(m.MetricId) || m.TimestampMs > latestByMetric[m.MetricId].TimestampMs)
                        latestByMetric[m.MetricId] = m;
                }

                foreach (var kvp in latestByMetric)
                {
                    var def = GetMetricDef(kvp.Key);
                    string unit = def.Unit == MetricUnit.Milliseconds ? "ms" :
                                  def.Unit == MetricUnit.Bytes ? "bytes" : "";
                    sb.AppendLine($"  {def.Name}: {kvp.Value.Value:F2} {unit}");
                }
                sb.AppendLine();
            }

            // ── Lifecycle ─────────────────────────────────────
            if (incident.LifecycleRecords.Count > 0)
            {
                sb.AppendLine(DebugLocale.Get("section.lifecycle"));
                foreach (var rec in incident.LifecycleRecords)
                {
                    string phase = DebugLocale.GetPhaseName(rec.PhaseEnum);
                    string typeName = RuntimeDebugger.GetEventName(rec.TypeNameHash);
                    string taskInfo = rec.RelatedTaskId >= 0 ? $" → Task#{rec.RelatedTaskId}" : "";
                    sb.AppendLine($"  Obj#{rec.ObjectId} ({typeName}) {phase} [F{rec.Frame}]{taskInfo}");
                }
                sb.AppendLine();
            }

            // ── Async ────────────────────────────────────────
            if (incident.AsyncRecords.Count > 0)
            {
                sb.AppendLine(DebugLocale.Get("section.async"));
                foreach (var rec in incident.AsyncRecords)
                {
                    string status = DebugLocale.GetAsyncStatus(rec.StatusEnum);
                    string op = RuntimeDebugger.GetEventName(rec.OperationHash);
                    string race = rec.OwnerDestroyedBeforeComplete ? " " + DebugLocale.Get("async.raceCondition") : "";
                    sb.AppendLine($"  Task#{rec.TaskId} ({op}) Owner:Obj#{rec.OwnerObjectId} [{status}]{race}");
                    if (rec.OwnerDestroyedBeforeComplete)
                    {
                        sb.AppendLine($"    Owner destroyed at frame {rec.OwnerDestroyedFrame}, task completed at frame {rec.CompleteFrame}");
                    }
                }
                sb.AppendLine();
            }

            // ── Resources ─────────────────────────────────────
            if (incident.ResourceRecords.Count > 0)
            {
                sb.AppendLine(DebugLocale.Get("section.resources"));
                int resCount = Math.Min(incident.ResourceRecords.Count, maxEvents);
                for (int i = 0; i < resCount; i++)
                {
                    var rec = incident.ResourceRecords[i];
                    string op = DebugLocale.GetResourceOperation(rec.OperationEnum);
                    string path = RuntimeDebugger.GetEventName(rec.AssetPathHash);
                    sb.AppendLine($"  {op} \"{path}\" [F{rec.Frame}]");
                }
                sb.AppendLine();
            }

            // ── Events ────────────────────────────────────────
            if (incident.Events.Count > 0)
            {
                sb.AppendLine(DebugLocale.Get("section.events"));
                int evtCount = Math.Min(incident.Events.Count, maxEvents);
                for (int i = 0; i < evtCount; i++)
                {
                    var evt = incident.Events[i];
                    string name = RuntimeDebugger.GetEventName(evt.EventHash);
                    sb.AppendLine($"  {name} [F{evt.Frame}] ctx={evt.ContextId}");
                }
                if (incident.Events.Count > maxEvents)
                    sb.AppendLine($"... ({incident.Events.Count - maxEvents} more events)");
            }

            return sb.ToString();
        }

        private static MetricDefinition GetMetricDef(int metricId)
        {
            switch (metricId)
            {
                case MetricIds.FrameTime: return PerformanceMonitor.FrameTimeDef;
                case MetricIds.CPUTime: return PerformanceMonitor.CPUTimeDef;
                case MetricIds.GPUTime: return PerformanceMonitor.GPUTimeDef;
                case MetricIds.GCAlloc: return PerformanceMonitor.GCAllocDef;
                case MetricIds.ManagedMemory: return PerformanceMonitor.ManagedMemoryDef;
                case MetricIds.DrawCalls: return PerformanceMonitor.DrawCallsDef;
                case MetricIds.SetPass: return PerformanceMonitor.SetPassDef;
                case MetricIds.Batches: return PerformanceMonitor.BatchesDef;
                default: return new MetricDefinition(metricId, $"Metric_{metricId}", "", MetricUnit.Count);
            }
        }
    }
}
