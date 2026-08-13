using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeDebugger.Editor
{
    public class RuntimeDebuggerWindow : EditorWindow
    {
        private const string MenuPath = "Window/Analysis/Runtime Debugger";

        // Layout elements
        private VisualElement _leftPanel;
        private VisualElement _rightPanel;
        private Label _statusLabel;
        private Label _statLabel;
        private float _refreshTimer;
        private string _selectedCategory = "overview";
        private int _selectedIndex = -1;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var w = GetWindow<RuntimeDebuggerWindow>("Runtime Debugger");
            w.minSize = new Vector2(700, 450);
        }

        private void CreateGUI()
        {
            rootVisualElement.style.backgroundColor = new Color(0.21f, 0.21f, 0.21f);
            BuildLayout();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!RuntimeDebugger.IsInitialized) return;

            if (!EditorApplication.isPlaying)
                RuntimeDebugger.OnFrameUpdate();

            // Auto-export pending incidents
            if (RuntimeDebugger.Triggers?.HasPendingIncident == true)
            {
                var incident = RuntimeDebugger.Triggers.RetrieveIncident();
                if (incident != null)
                {
                    IncidentExporter.Export(incident);
                    Debug.Log("[RuntimeDebugger] Incident auto-exported");
                }
            }

            // Auto-refresh UI every 0.2s during play
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= 0.2f)
            {
                _refreshTimer = 0;
                RefreshUI();
            }
        }

        // ── Layout ────────────────────────────────────────────

        private void BuildLayout()
        {
            rootVisualElement.Clear();

            // Toolbar
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingRight = 6;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);

            _statusLabel = new Label("Not Initialized");
            _statusLabel.style.color = new Color(0.9f, 0.4f, 0.4f);
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusLabel.style.flexGrow = 1;
            toolbar.Add(_statusLabel);

            var enBtn = new Button(() => { DebugLocale.SetLanguage(DebugLanguage.English); RefreshUI(); })
            { text = "EN" };
            enBtn.style.fontSize = 9;
            toolbar.Add(enBtn);

            var zhBtn = new Button(() => { DebugLocale.SetLanguage(DebugLanguage.Chinese); RefreshUI(); })
            { text = "中文" };
            zhBtn.style.fontSize = 9;
            toolbar.Add(zhBtn);

            var initBtn = new Button(() => { if (!RuntimeDebugger.IsInitialized) RuntimeDebugger.Initialize(); RefreshUI(); })
            { text = "Init" };
            toolbar.Add(initBtn);

            rootVisualElement.Add(toolbar);

            // Control bar
            var ctrlBar = new VisualElement();
            ctrlBar.style.flexDirection = FlexDirection.Row;
            ctrlBar.style.paddingLeft = 6;
            ctrlBar.style.paddingBottom = 4;
            ctrlBar.style.paddingTop = 2;

            var modeField = new EnumField(DebugMode.TriggerDebug);
            modeField.style.width = 120;
            modeField.style.marginRight = 4;
            ctrlBar.Add(modeField);

            var durLbl = new Label("Duration:") { style = { alignSelf = Align.Center, marginRight = 2 } };
            ctrlBar.Add(durLbl);
            var durField = new IntegerField(5) { value = 5, style = { width = 40, marginRight = 4 } };
            ctrlBar.Add(durField);

            var startBtn = new Button(() =>
            {
                if (!RuntimeDebugger.IsInitialized) RuntimeDebugger.Initialize();
                var mode = (DebugMode)modeField.value;
                RuntimeDebugger.StartSession(mode, mode == DebugMode.DeepDebug ? durField.value : 0);
                RefreshUI();
            })
            { text = "▶ Start" };
            startBtn.style.backgroundColor = new Color(0.15f, 0.4f, 0.15f);
            ctrlBar.Add(startBtn);

            var stopBtn = new Button(() => { RuntimeDebugger.StopSession(); RefreshUI(); })
            { text = "⬛ Stop" };
            stopBtn.style.backgroundColor = new Color(0.4f, 0.15f, 0.15f);
            ctrlBar.Add(stopBtn);

            var captureBtn = new Button(() =>
            {
                if (!RuntimeDebugger.IsInitialized) return;
                var inc = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Manual capture");
                IncidentExporter.Export(inc);
                RefreshUI();
            })
            { text = "📷 Capture" };
            ctrlBar.Add(captureBtn);

            rootVisualElement.Add(ctrlBar);

            // Stat bar
            _statLabel = new Label("");
            _statLabel.style.paddingLeft = 6;
            _statLabel.style.paddingBottom = 2;
            _statLabel.style.fontSize = 10;
            _statLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            rootVisualElement.Add(_statLabel);

            // Main content: two-panel split
            var split = new VisualElement();
            split.style.flexDirection = FlexDirection.Row;
            split.style.flexGrow = 1;

            // Left sidebar
            _leftPanel = new ScrollView();
            _leftPanel.style.width = 200;
            _leftPanel.style.borderRightWidth = 1;
            _leftPanel.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);
            _leftPanel.style.paddingTop = 4;
            split.Add(_leftPanel);

            // Right detail
            _rightPanel = new ScrollView();
            _rightPanel.style.flexGrow = 1;
            _rightPanel.style.paddingTop = 4;
            _rightPanel.style.paddingLeft = 8;
            _rightPanel.style.paddingRight = 8;
            _rightPanel.style.paddingBottom = 8;
            split.Add(_rightPanel);

            rootVisualElement.Add(split);

            RefreshUI();
        }

        // ── UI Refresh ────────────────────────────────────────

        private void RefreshUI()
        {
            UpdateStatusBar();
            BuildSidebar();
            BuildDetail();
        }

        private void UpdateStatusBar()
        {
            if (_statusLabel == null) return;

            if (RuntimeDebugger.IsInitialized)
            {
                _statusLabel.text = $"● Ready  |  Mode: {RuntimeDebugger.Mode}";
                _statusLabel.style.color = new Color(0.3f, 0.8f, 0.3f);

                _statLabel.text =
                    $"Events: {RuntimeDebugger.EventCount}  |  " +
                    $"Traces: {RuntimeDebugger.TraceCount}  |  " +
                    $"Objects: {RuntimeDebugger.Lifecycle?.TrackedCount ?? 0}  |  " +
                    $"Async: {RuntimeDebugger.Async?.ActiveTaskCount ?? 0} active  |  " +
                    $"Resources: {RuntimeDebugger.Resource?.PendingLoadCount ?? 0} pending  |  " +
                    $"Metrics: {RuntimeDebugger.Performance?.SampleCount ?? 0} samples";
            }
            else
            {
                _statusLabel.text = "● Not Initialized — click Init";
                _statusLabel.style.color = new Color(0.9f, 0.4f, 0.4f);
                _statLabel.text = "";
            }
        }

        // ── Sidebar ───────────────────────────────────────────

        private void BuildSidebar()
        {
            if (_leftPanel == null) return;
            _leftPanel.Clear();

            if (!RuntimeDebugger.IsInitialized)
            {
                _leftPanel.Add(new Label("Click Init to start.") { style = { padding = 8, color = new Color(0.6f,0.6f,0.6f) } });
                return;
            }

            AddSidebarItem("overview", "📊 Overview", "");
            AddSidebarItem("timeline", "🕐 Timeline", $"{RuntimeDebugger.TraceCount}");
            AddSidebarItem("events", "📋 Events", $"{RuntimeDebugger.EventCount}");
            AddSidebarItem("metrics", "📈 Metrics", $"{RuntimeDebugger.Performance?.SampleCount ?? 0}");
            AddSidebarItem("lifecycle", "🔄 Lifecycle", $"{RuntimeDebugger.Lifecycle?.RecordCount ?? 0}");
            AddSidebarItem("async", "⚡ Async", $"{RuntimeDebugger.Async?.CompletedCount ?? 0}");
            AddSidebarItem("resources", "📦 Resources", $"{RuntimeDebugger.Resource?.RecordCount ?? 0}");
            AddSidebarItem("incidents", "🚨 Incidents", $"{(RuntimeDebugger.Triggers?.HasPendingIncident == true ? "1!" : "0")}");

            // Trigger state
            _leftPanel.Add(new VisualElement { style = { height = 8 } });
            var triggerState = new Label($"Trigger: {RuntimeDebugger.Triggers?.CurrentState ?? TriggerSystem.State.Idle}");
            triggerState.style.fontSize = 9;
            triggerState.style.color = new Color(0.5f, 0.5f, 0.5f);
            triggerState.style.paddingLeft = 8;
            _leftPanel.Add(triggerState);
        }

        private void AddSidebarItem(string id, string label, string count)
        {
            var row = new Button(() => { _selectedCategory = id; _selectedIndex = -1; BuildDetail(); });
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingLeft = 8;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.borderLeftWidth = 3;
            row.style.borderLeftColor = _selectedCategory == id
                ? new Color(0.3f, 0.6f, 1.0f)
                : new Color(0, 0, 0, 0);
            row.style.backgroundColor = _selectedCategory == id
                ? new Color(0.28f, 0.28f, 0.32f)
                : new Color(0, 0, 0, 0);

            var nameLbl = new Label(label) { style = { flexGrow = 1, fontSize = 11 } };
            row.Add(nameLbl);

            if (!string.IsNullOrEmpty(count))
            {
                var countLbl = new Label(count) { style = { fontSize = 9, color = new Color(0.6f, 0.6f, 0.6f) } };
                row.Add(countLbl);
            }

            _leftPanel.Add(row);
        }

        // ── Detail Panel ──────────────────────────────────────

        private void BuildDetail()
        {
            if (_rightPanel == null) return;
            _rightPanel.Clear();

            if (!RuntimeDebugger.IsInitialized)
            {
                _rightPanel.Add(new Label("Click Init in toolbar to start monitoring.") { style = { padding = 16, color = new Color(0.6f,0.6f,0.6f) } });
                return;
            }

            switch (_selectedCategory)
            {
                case "overview": BuildOverview(); break;
                case "timeline": BuildTimeline(); break;
                case "events": BuildEvents(); break;
                case "metrics": BuildMetrics(); break;
                case "lifecycle": BuildLifecycle(); break;
                case "async": BuildAsync(); break;
                case "resources": BuildResources(); break;
                case "incidents": BuildIncidents(); break;
            }
        }

        // ── Overview ──────────────────────────────────────────

        private void BuildOverview()
        {
            AddHeader("📊 Overview");

            AddStatCard("Events", RuntimeDebugger.EventCount, new Color(0.3f, 0.7f, 0.3f));
            AddStatCard("Traces", RuntimeDebugger.TraceCount, new Color(0.3f, 0.5f, 0.8f));
            AddStatCard("Lifecycle Records", RuntimeDebugger.Lifecycle?.RecordCount ?? 0, new Color(0.7f, 0.5f, 0.3f));
            AddStatCard("Async Tasks (active)", RuntimeDebugger.Async?.ActiveTaskCount ?? 0, new Color(0.8f, 0.6f, 0.2f));
            AddStatCard("Resources (pending)", RuntimeDebugger.Resource?.PendingLoadCount ?? 0, new Color(0.6f, 0.3f, 0.6f));
            AddStatCard("Metric Samples", RuntimeDebugger.Performance?.SampleCount ?? 0, new Color(0.3f, 0.6f, 0.6f));

            AddSpacer(8);

            // Latest metrics snapshot
            AddSection("Latest Metrics");
            if (RuntimeDebugger.Performance != null)
            {
                foreach (var def in RuntimeDebugger.Performance.Definitions)
                {
                    double val = RuntimeDebugger.Performance.GetLatestValue(def.Id);
                    string display = def.Unit == MetricUnit.Bytes
                        ? FormatBytes((long)val)
                        : $"{val:F2} ms";
                    AddMetricBar(DebugLocale.GetMetricName(def.Id), display, def.Unit == MetricUnit.Bytes ? (float)val / 10485760f : (float)val / 50f);
                }
            }

            AddSpacer(8);

            // Warnings
            AddSection("⚠ Detected Issues");
            var racing = RuntimeDebugger.Async?.GetRacingTasks();
            if (racing != null && racing.Count > 0)
            {
                foreach (var r in racing)
                {
                    var op = RuntimeDebugger.GetEventName(r.OperationHash);
                    AddWarning($"Async race: Task#{r.TaskId} ({op}) — owner destroyed before completion");
                }
            }
            else
            {
                AddInfo("No issues detected");
            }

            var dups = RuntimeDebugger.Resource?.GetDuplicateLoads(50);
            if (dups != null && dups.Count > 0)
            {
                foreach (var d in dups)
                {
                    var path = RuntimeDebugger.GetEventName(d.AssetPathHash);
                    AddWarning($"Duplicate load: \"{path}\" at frame {d.Frame}");
                }
            }
        }

        // ── Timeline ──────────────────────────────────────────

        private void BuildTimeline()
        {
            AddHeader("🕐 Timeline");

            var traces = RuntimeDebugger.GetTraceTree();
            if (traces.Length == 0)
            {
                AddInfo("No trace data. Play the game to collect traces.");
                return;
            }

            // Build tree from flat array
            var byId = new Dictionary<int, TraceNode>();
            var children = new Dictionary<int, List<TraceNode>>();
            var roots = new List<TraceNode>();

            foreach (var t in traces)
            {
                byId[t.NodeId] = t;
                if (t.ParentId < 0 || !byId.ContainsKey(t.ParentId))
                    roots.Add(t);
                else
                {
                    if (!children.ContainsKey(t.ParentId))
                        children[t.ParentId] = new List<TraceNode>();
                    children[t.ParentId].Add(t);
                }
            }

            // Recalculate children based on completion order
            // Nodes are in buffer in completion order, so parent appears after children
            // Rebuild properly: scan all nodes, group by parent
            children.Clear();
            roots.Clear();
            foreach (var t in traces)
            {
                if (t.ParentId < 0)
                    roots.Add(t);
                else
                {
                    if (!children.ContainsKey(t.ParentId))
                        children[t.ParentId] = new List<TraceNode>();
                    children[t.ParentId].Add(t);
                }
            }

            foreach (var root in roots)
                AddTraceNode(root, children, 0);
        }

        private void AddTraceNode(TraceNode node, Dictionary<int, List<TraceNode>> children, int depth)
        {
            var name = RuntimeDebugger.GetEventName(node.EventHash);
            var dur = node.IsFinished ? $"{node.DurationMs}ms" : "active";
            var color = node.DurationMs > 20 ? new Color(0.9f, 0.3f, 0.3f)
                       : node.DurationMs > 5 ? new Color(0.9f, 0.8f, 0.2f)
                       : new Color(0.3f, 0.8f, 0.3f);

            var hasChildren = children.TryGetValue(node.NodeId, out var kids) && kids.Count > 0;

            var foldout = new Foldout { text = $"{name}  [F{node.Frame}]  {dur}", value = true };
            foldout.style.paddingLeft = depth * 16;
            foldout.style.paddingTop = 1;
            foldout.style.paddingBottom = 1;

            // Duration bar
            if (node.IsFinished)
            {
                var barRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2, marginBottom = 4 } };
                var bar = new VisualElement();
                bar.style.height = 4;
                bar.style.width = Mathf.Clamp((float)node.DurationMs * 3, 4, 200);
                bar.style.backgroundColor = color;
                bar.style.borderTopLeftRadius = 2;
                bar.style.borderTopRightRadius = 2;
                barRow.Add(bar);
                foldout.Add(barRow);
            }

            // Detail
            var detail = new Label($"Frame: {node.Frame}  |  Start: {node.StartMs}ms  |  Duration: {(node.IsFinished ? node.DurationMs + "ms" : "active")}  |  Children: {(kids?.Count ?? 0)}");
            detail.style.fontSize = 9;
            detail.style.color = new Color(0.6f, 0.6f, 0.6f);
            detail.style.paddingBottom = 4;
            foldout.Add(detail);

            if (hasChildren)
            {
                foreach (var kid in kids)
                    AddTraceNode(kid, children, depth + 1);
            }

            _rightPanel.Add(foldout);
        }

        // ── Events ────────────────────────────────────────────

        private void BuildEvents()
        {
            AddHeader("📋 Events");

            var events = RuntimeDebugger.GetEvents();
            if (events.Length == 0)
            {
                AddInfo("No events recorded. Play the game to collect events.");
                return;
            }

            // Show last 200 events (most recent at top)
            int start = Mathf.Max(0, events.Length - 200);
            for (int i = events.Length - 1; i >= start; i--)
            {
                var evt = events[i];
                var name = RuntimeDebugger.GetEventName(evt.EventHash);

                Color color;
                if (name.Contains("Error") || name.Contains("Exception"))
                    color = new Color(0.9f, 0.3f, 0.3f);
                else if (name.Contains("Warning"))
                    color = new Color(0.9f, 0.8f, 0.2f);
                else if (name.Contains("Object.Create"))
                    color = new Color(0.3f, 0.8f, 0.4f);
                else if (name.Contains("Object.Destroy"))
                    color = new Color(0.8f, 0.5f, 0.3f);
                else if (name.Contains("Object.Disable"))
                    color = new Color(0.7f, 0.5f, 0.3f);
                else if (name.Contains("Object.Enable"))
                    color = new Color(0.3f, 0.7f, 0.5f);
                else
                    color = new Color(0.7f, 0.7f, 0.7f);

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 2, paddingTop = 2, borderBottomWidth = 1, borderBottomColor = new Color(0.26f, 0.26f, 0.26f) } };

                var frameLbl = new Label($"[F{evt.Frame}]") { style = { width = 60, color = new Color(0.5f, 0.5f, 0.5f), fontSize = 10 } };
                row.Add(frameLbl);

                var nameLbl = new Label(name) { style = { flexGrow = 1, color = color, fontSize = 10 } };
                row.Add(nameLbl);

                var ctxLbl = new Label(evt.ContextId >= 0 ? $"ctx:{evt.ContextId}" : "") { style = { width = 60, color = new Color(0.4f, 0.4f, 0.4f), fontSize = 9 } };
                row.Add(ctxLbl);

                _rightPanel.Add(row);
            }

            if (events.Length > 200)
                AddInfo($"Showing last 200 of {events.Length} events");
        }

        // ── Metrics ───────────────────────────────────────────

        private void BuildMetrics()
        {
            AddHeader("📈 Performance Metrics");

            if (RuntimeDebugger.Performance == null)
            {
                AddInfo("No performance monitor.");
                return;
            }

            // Latest values with bars
            AddSection("Latest Values");
            foreach (var def in RuntimeDebugger.Performance.Definitions)
            {
                double val = RuntimeDebugger.Performance.GetLatestValue(def.Id);
                string display = def.Unit == MetricUnit.Bytes
                    ? FormatBytes((long)val)
                    : $"{val:F2} ms";
                float ratio = def.Unit == MetricUnit.Bytes ? (float)val / 10485760f : (float)val / 50f;
                AddMetricBar(DebugLocale.GetMetricName(def.Id), display, ratio);
            }

            AddSpacer(8);

            // Metric history (last N samples per metric)
            AddSection("Recent History (last 50 samples)");
            var allMetrics = RuntimeDebugger.GetPerfMetrics();
            if (allMetrics.Length == 0)
            {
                AddInfo("No samples yet. Start a session to collect metrics.");
                return;
            }

            // Group by metric ID
            var byMetric = new Dictionary<int, List<PerfMetric>>();
            foreach (var m in allMetrics)
            {
                if (!byMetric.ContainsKey(m.MetricId))
                    byMetric[m.MetricId] = new List<PerfMetric>();
                byMetric[m.MetricId].Add(m);
            }

            foreach (var kvp in byMetric)
            {
                var samples = kvp.Value;
                var def = GetMetricDef(kvp.Key);
                var name = DebugLocale.GetMetricName(kvp.Key);

                var foldout = new Foldout { text = $"{name} ({samples.Count} samples)", value = false };
                foldout.style.paddingTop = 2;
                foldout.style.paddingBottom = 2;

                int showCount = Mathf.Min(samples.Count, 50);
                int startIdx = samples.Count - showCount;
                for (int i = startIdx; i < samples.Count; i++)
                {
                    var s = samples[i];
                    string display = def.Unit == MetricUnit.Bytes
                        ? FormatBytes((long)s.Value)
                        : $"{s.Value:F2} ms";
                    var lbl = new Label($"  [F{s.Frame}] {display}") { style = { fontSize = 9, color = new Color(0.7f,0.7f,0.7f), paddingBottom = 1 } };
                    foldout.Add(lbl);
                }

                _rightPanel.Add(foldout);
            }
        }

        // ── Lifecycle ─────────────────────────────────────────

        private void BuildLifecycle()
        {
            AddHeader("🔄 Object Lifecycle");

            var records = RuntimeDebugger.GetLifecycleRecords();
            if (records.Length == 0)
            {
                AddInfo("No lifecycle records. Play the game — scene objects are auto-tracked.");
                return;
            }

            // Group by ObjectId
            var byObj = new Dictionary<int, List<LifecycleRecord>>();
            foreach (var r in records)
            {
                if (!byObj.ContainsKey(r.ObjectId))
                    byObj[r.ObjectId] = new List<LifecycleRecord>();
                byObj[r.ObjectId].Add(r);
            }

            foreach (var kvp in byObj)
            {
                var typeName = RuntimeDebugger.GetEventName(kvp.Value[0].TypeNameHash);
                var alive = RuntimeDebugger.Lifecycle?.IsAlive(kvp.Key) ?? false;
                var statusIcon = alive ? "🟢" : "🔴";

                var foldout = new Foldout { text = $"{statusIcon} Obj#{kvp.Key} — {typeName}", value = false };
                foldout.style.paddingTop = 2;
                foldout.style.paddingBottom = 2;

                // Timeline
                foreach (var rec in kvp.Value)
                {
                    var phase = DebugLocale.GetPhaseName(rec.PhaseEnum);
                    var color = rec.PhaseEnum == LifecyclePhase.Create ? new Color(0.3f, 0.8f, 0.3f)
                               : rec.PhaseEnum == LifecyclePhase.Destroy ? new Color(0.9f, 0.3f, 0.3f)
                               : new Color(0.6f, 0.6f, 0.6f);
                    var lbl = new Label($"  [{phase}] Frame {rec.Frame}  @ {rec.TimestampMs}ms") { style = { fontSize = 10, color = color, paddingBottom = 1 } };
                    foldout.Add(lbl);

                    if (rec.RelatedTaskId >= 0)
                    {
                        var warn = new Label($"    ⚠ Related to Task#{rec.RelatedTaskId} (async race condition)") { style = { fontSize = 9, color = new Color(0.9f,0.5f,0.2f) } };
                        foldout.Add(warn);
                    }
                }

                _rightPanel.Add(foldout);
            }
        }

        // ── Async ─────────────────────────────────────────────

        private void BuildAsync()
        {
            AddHeader("⚡ Async Tasks");

            var records = RuntimeDebugger.GetAsyncRecords();
            if (records.Length == 0)
            {
                AddInfo("No async tasks recorded.");
                return;
            }

            // Racing tasks first (warnings)
            var racing = RuntimeDebugger.Async?.GetRacingTasks();
            if (racing != null && racing.Count > 0)
            {
                AddSection($"⚠ Race Conditions ({racing.Count})");
                foreach (var r in racing)
                {
                    var op = RuntimeDebugger.GetEventName(r.OperationHash);
                    AddWarning($"Task#{r.TaskId} ({op}) Owner:Obj#{r.OwnerObjectId} — Owner destroyed at F{r.OwnerDestroyedFrame}, task completed at F{r.CompleteFrame}");
                }
                AddSpacer(8);
            }

            AddSection("All Tasks");
            foreach (var rec in records)
            {
                var op = RuntimeDebugger.GetEventName(rec.OperationHash);
                var status = DebugLocale.GetAsyncStatus(rec.StatusEnum);
                var hasRace = rec.OwnerDestroyedBeforeComplete;

                var foldout = new Foldout
                {
                    text = $"{(hasRace ? "⚠ " : "")}Task#{rec.TaskId} — {op} [{status}]",
                    value = false
                };
                foldout.style.paddingTop = 2;
                foldout.style.paddingBottom = 2;

                var info = new Label(
                    $"  Owner: Obj#{rec.OwnerObjectId}\n" +
                    $"  Start: F{rec.StartFrame} @ {rec.StartMs}ms\n" +
                    $"  Complete: {(rec.CompleteFrame >= 0 ? $"F{rec.CompleteFrame} @ {rec.CompleteMs}ms" : "N/A")}\n" +
                    $"  Status: {status}" +
                    (hasRace ? $"\n  ⚠ Owner destroyed at F{rec.OwnerDestroyedFrame} before task completed" : ""));
                info.style.fontSize = 10;
                info.style.color = hasRace ? new Color(0.9f, 0.6f, 0.3f) : new Color(0.7f, 0.7f, 0.7f);
                foldout.Add(info);

                _rightPanel.Add(foldout);
            }
        }

        // ── Resources ─────────────────────────────────────────

        private void BuildResources()
        {
            AddHeader("📦 Resource Operations");

            var records = RuntimeDebugger.GetResourceRecords();
            if (records.Length == 0)
            {
                AddInfo("No resource operations recorded.");
                return;
            }

            // Duplicate detection
            var dups = RuntimeDebugger.Resource?.GetDuplicateLoads(100);
            if (dups != null && dups.Count > 0)
            {
                AddSection($"⚠ Duplicate Loads ({dups.Count})");
                foreach (var d in dups)
                {
                    var path = RuntimeDebugger.GetEventName(d.AssetPathHash);
                    AddWarning($"\"{path}\" loaded again at F{d.Frame}");
                }
                AddSpacer(8);
            }

            AddSection("All Operations");
            foreach (var rec in records)
            {
                var op = DebugLocale.GetResourceOperation(rec.OperationEnum);
                var path = RuntimeDebugger.GetEventName(rec.AssetPathHash);

                var color = rec.OperationEnum == ResourceOperation.LoadStart ? new Color(0.3f, 0.7f, 0.3f)
                           : rec.OperationEnum == ResourceOperation.LoadFail ? new Color(0.9f, 0.3f, 0.3f)
                           : rec.OperationEnum == ResourceOperation.Release ? new Color(0.6f, 0.6f, 0.3f)
                           : new Color(0.3f, 0.6f, 0.8f);

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 2, paddingTop = 2, borderBottomWidth = 1, borderBottomColor = new Color(0.26f,0.26f,0.26f) } };

                var frameLbl = new Label($"[F{rec.Frame}]") { style = { width = 60, color = new Color(0.5f,0.5f,0.5f), fontSize = 10 } };
                row.Add(frameLbl);

                var opLbl = new Label(op) { style = { width = 90, color = color, fontSize = 10 } };
                row.Add(opLbl);

                var pathLbl = new Label(path) { style = { flexGrow = 1, fontSize = 10 } };
                row.Add(pathLbl);

                _rightPanel.Add(row);
            }
        }

        // ── Incidents ─────────────────────────────────────────

        private void BuildIncidents()
        {
            AddHeader("🚨 Incidents");

            // Check for pending incident
            if (RuntimeDebugger.Triggers?.HasPendingIncident == true)
            {
                var inc = RuntimeDebugger.Triggers.PendingIncident;
                AddSection("Pending Incident (auto-captured)");

                var detail = new Label(
                    $"Type: {inc.Type}\n" +
                    $"Frame: {inc.TriggerFrame}\n" +
                    $"Description: {inc.TriggerDescription}\n" +
                    $"Pre-trace: {inc.PreTrace.Count} nodes\n" +
                    $"Events: {inc.Events.Count}\n" +
                    $"Lifecycle: {inc.LifecycleRecords.Count}\n" +
                    $"Async: {inc.AsyncRecords.Count}\n" +
                    $"Resources: {inc.ResourceRecords.Count}\n" +
                    $"Metrics: {inc.Metrics.Count}");
                detail.style.whiteSpace = WhiteSpace.Normal;
                detail.style.paddingBottom = 8;
                _rightPanel.Add(detail);

                var exportBtn = new Button(() =>
                {
                    var incident = RuntimeDebugger.Triggers.RetrieveIncident();
                    if (incident != null)
                    {
                        var path = IncidentExporter.Export(incident);
                        Debug.Log($"[RuntimeDebugger] Exported to: {path}");
                    }
                    RefreshUI();
                })
                { text = "📁 Export & Clear" };
                exportBtn.style.height = 32;
                _rightPanel.Add(exportBtn);

                AddSpacer(8);
            }

            AddSection("Manual Capture");
            var captureBtn = new Button(() =>
            {
                if (!RuntimeDebugger.IsInitialized) return;
                var inc = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Manual capture");
                var path = IncidentExporter.Export(inc);
                Debug.Log($"[RuntimeDebugger] Exported to: {path}");
            })
            { text = "📷 Capture Current State" };
            captureBtn.style.height = 36;
            captureBtn.style.fontSize = 13;
            _rightPanel.Add(captureBtn);

            AddSpacer(8);
            AddSection("AI Analysis");
            var aiBtn = new Button(() =>
            {
                if (!RuntimeDebugger.IsInitialized) return;
                var inc = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "AI analysis");
                var promptPath = AIPromptBuilder.BuildAndSavePrompt(inc, Application.persistentDataPath + "/AI_Prompts");
                Debug.Log($"[RuntimeDebugger] AI Prompt saved to: {promptPath}");
                EditorGUIUtility.systemCopyBuffer = System.IO.File.ReadAllText(promptPath);
                Debug.Log("[RuntimeDebugger] Prompt copied to clipboard. Paste into Claude/ChatGPT.");
            })
            { text = "🤖 Generate AI Prompt (copied to clipboard)" };
            aiBtn.style.height = 36;
            aiBtn.style.fontSize = 12;
            _rightPanel.Add(aiBtn);
        }

        // ── UI Helpers ───────────────────────────────────────

        private void AddHeader(string text)
        {
            var header = new Label(text);
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.8f, 0.8f, 0.85f);
            header.style.paddingBottom = 8;
            header.style.paddingTop = 4;
            _rightPanel.Add(header);

            var line = new VisualElement();
            line.style.height = 1;
            line.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f);
            line.style.marginBottom = 8;
            _rightPanel.Add(line);
        }

        private void AddSection(string text)
        {
            var section = new Label(text);
            section.style.fontSize = 12;
            section.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.style.color = new Color(0.7f, 0.7f, 0.75f);
            section.style.paddingTop = 4;
            section.style.paddingBottom = 4;
            _rightPanel.Add(section);
        }

        private void AddStatCard(string label, int count, Color color)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.paddingLeft = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.marginBottom = 2;
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = color;
            card.style.backgroundColor = new Color(0.25f, 0.25f, 0.27f);

            var nameLbl = new Label(label) { style = { flexGrow = 1, fontSize = 11, color = new Color(0.8f,0.8f,0.8f) } };
            card.Add(nameLbl);

            var countLbl = new Label(count.ToString()) { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, color = color, width = 50 } };
            card.Add(countLbl);

            _rightPanel.Add(card);
        }

        private void AddMetricBar(string name, string value, float ratio)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 3, paddingTop = 3, alignItems = Align.Center } };

            var nameLbl = new Label(name) { style = { width = 130, fontSize = 10, color = new Color(0.8f,0.8f,0.8f) } };
            row.Add(nameLbl);

            var barBg = new VisualElement();
            barBg.style.width = 150;
            barBg.style.height = 8;
            barBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            barBg.style.borderTopLeftRadius = 4;
            barBg.style.borderTopRightRadius = 4;
            barBg.style.marginRight = 6;

            var barFill = new VisualElement();
            barFill.style.width = Mathf.Clamp(ratio * 150, 2, 150);
            barFill.style.height = 8;
            barFill.style.backgroundColor = ratio > 0.8f ? new Color(0.9f, 0.3f, 0.3f)
                                       : ratio > 0.5f ? new Color(0.9f, 0.8f, 0.2f)
                                       : new Color(0.3f, 0.7f, 0.3f);
            barFill.style.borderTopLeftRadius = 4;
            barFill.style.borderTopRightRadius = 4;
            barBg.Add(barFill);
            row.Add(barBg);

            var valLbl = new Label(value) { style = { fontSize = 10, color = new Color(0.7f,0.7f,0.7f), width = 80 } };
            row.Add(valLbl);

            _rightPanel.Add(row);
        }

        private void AddWarning(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingLeft = 8;
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;
            row.style.borderLeftWidth = 3;
            row.style.borderLeftColor = new Color(0.9f, 0.5f, 0.2f);
            row.style.marginBottom = 2;

            var lbl = new Label("⚠ " + text) { style = { fontSize = 10, color = new Color(0.9f, 0.7f, 0.4f), whiteSpace = WhiteSpace.Normal } };
            row.Add(lbl);
            _rightPanel.Add(row);
        }

        private void AddInfo(string text)
        {
            var lbl = new Label(text) { style = { fontSize = 10, color = new Color(0.5f, 0.5f, 0.5f), paddingBottom = 4, paddingLeft = 8 } };
            _rightPanel.Add(lbl);
        }

        private void AddSpacer(int height)
        {
            _rightPanel.Add(new VisualElement { style = { height = height } });
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1048576.0:F2} MB";
        }

        private static MetricDefinition GetMetricDef(int metricId)
        {
            return metricId switch
            {
                MetricIds.FrameTime => PerformanceMonitor.FrameTimeDef,
                MetricIds.CPUTime => PerformanceMonitor.CPUTimeDef,
                MetricIds.GPUTime => PerformanceMonitor.GPUTimeDef,
                MetricIds.GCAlloc => PerformanceMonitor.GCAllocDef,
                MetricIds.ManagedMemory => PerformanceMonitor.ManagedMemoryDef,
                MetricIds.DrawCalls => PerformanceMonitor.DrawCallsDef,
                MetricIds.SetPass => PerformanceMonitor.SetPassDef,
                MetricIds.Batches => PerformanceMonitor.BatchesDef,
                _ => new MetricDefinition(metricId, $"Metric_{metricId}", "", MetricUnit.Count),
            };
        }
    }
}
