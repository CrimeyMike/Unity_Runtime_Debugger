using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeDebugger.Editor
{
    public class RuntimeDebuggerWindow : EditorWindow
    {
        private const string MenuPath = "Window/Analysis/Runtime Debugger";

        private VisualElement _tabContainer;
        private Label _statusLabel;
        private string _currentTab = "timeline";
        private RadioButton _perfToggle;
        private RadioButton _deepToggle;
        private RadioButton _triggerToggle;
        private IntegerField _durField;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<RuntimeDebuggerWindow>(DebugLocale.Get("window.title"));
            window.minSize = new Vector2(500, 400);
        }

        private void CreateGUI()
        {
            rootVisualElement.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            BuildUI();
            EditorApplication.update += OnEditorUpdate;
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // ── Header ─────────────────────────────────────────
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 6;
            header.style.paddingLeft = 8;
            header.style.paddingRight = 8;

            _statusLabel = new Label(DebugLocale.Get("status.notInitialized"));
            _statusLabel.style.color = Color.red;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusLabel.style.flexGrow = 1;
            header.Add(_statusLabel);

            // Language toggle
            var langRow = new VisualElement();
            langRow.style.flexDirection = FlexDirection.Row;
            langRow.style.alignItems = Align.Center;

            var langLabel = new Label(DebugLocale.Get("misc.language") + ":");
            langLabel.style.marginRight = 4;
            langRow.Add(langLabel);

            var enBtn = new Button(() => { DebugLocale.SetLanguage(DebugLanguage.English); BuildUI(); })
            { text = "EN", style = { fontSize = 10 } };
            enBtn.style.marginRight = 2;
            langRow.Add(enBtn);

            var zhBtn = new Button(() => { DebugLocale.SetLanguage(DebugLanguage.Chinese); BuildUI(); })
            { text = "中文", style = { fontSize = 10 } };
            langRow.Add(zhBtn);

            header.Add(langRow);

            var initBtn = new Button(OnInitialize) { text = DebugLocale.Get("btn.initialize") };
            header.Add(initBtn);

            rootVisualElement.Add(header);

            // ── Mode selector ──────────────────────────────────
            var modeRow = new VisualElement();
            modeRow.style.flexDirection = FlexDirection.Row;
            modeRow.style.paddingLeft = 8;
            modeRow.style.paddingBottom = 4;

            modeRow.Add(new Label(DebugLocale.Get("mode.label"))
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, alignSelf = Align.Center, marginRight = 8 }
            });

            _perfToggle = new RadioButton(DebugLocale.Get("mode.performance")) { name = "mode-perf" };
            _deepToggle = new RadioButton(DebugLocale.Get("mode.deepDebug")) { name = "mode-deep" };
            _triggerToggle = new RadioButton(DebugLocale.Get("mode.triggerDebug")) { name = "mode-trigger", value = true };
            var modeGroup = new RadioButtonGroup();
            modeGroup.Add(_perfToggle);
            modeGroup.Add(_deepToggle);
            modeGroup.Add(_triggerToggle);
            modeRow.Add(modeGroup);

            rootVisualElement.Add(modeRow);

            // ── Duration ───────────────────────────────────────
            var durRow = new VisualElement();
            durRow.style.flexDirection = FlexDirection.Row;
            durRow.style.paddingLeft = 8;
            durRow.style.paddingBottom = 4;
            durRow.Add(new Label(DebugLocale.Get("duration.label")));
            _durField = new IntegerField(5) { name = "duration", value = 5, style = { width = 60 } };
            durRow.Add(_durField);
            rootVisualElement.Add(durRow);

            // ── Buttons ────────────────────────────────────────
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.paddingLeft = 8;
            btnRow.style.paddingBottom = 8;

            var startBtn = new Button(() => OnStartSession(_deepToggle.value, _triggerToggle.value, _durField.value))
            { text = DebugLocale.Get("btn.start") };
            startBtn.style.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
            btnRow.Add(startBtn);

            var stopBtn = new Button(OnStopSession) { text = DebugLocale.Get("btn.stop") };
            stopBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            btnRow.Add(stopBtn);

            var captureBtn = new Button(OnCaptureIncident) { text = DebugLocale.Get("btn.capture") };
            btnRow.Add(captureBtn);

            rootVisualElement.Add(btnRow);

            // ── Separator ──────────────────────────────────────
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            sep.style.marginBottom = 4;
            rootVisualElement.Add(sep);

            // ── Tab bar ────────────────────────────────────────
            var tabRow = new VisualElement();
            tabRow.style.flexDirection = FlexDirection.Row;
            tabRow.style.paddingLeft = 8;
            tabRow.style.paddingBottom = 4;

            var timelineTab = new Button(() => ShowTab("timeline")) { text = DebugLocale.Get("tab.timeline") };
            var metricsTab = new Button(() => ShowTab("metrics")) { text = DebugLocale.Get("tab.metrics") };
            var incidentTab = new Button(() => ShowTab("incident")) { text = DebugLocale.Get("tab.incident") };
            tabRow.Add(timelineTab);
            tabRow.Add(metricsTab);
            tabRow.Add(incidentTab);

            rootVisualElement.Add(tabRow);

            // ── Tab container ──────────────────────────────────
            _tabContainer = new VisualElement();
            _tabContainer.style.flexGrow = 1;
            _tabContainer.style.paddingLeft = 8;
            _tabContainer.style.paddingRight = 8;
            _tabContainer.style.paddingBottom = 8;
            rootVisualElement.Add(_tabContainer);

            ShowTab(_currentTab);
            UpdateStatus();
        }

        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!RuntimeDebugger.IsInitialized) return;

            if (!EditorApplication.isPlaying)
            {
                RuntimeDebugger.OnFrameUpdate();
            }

            if (RuntimeDebugger.Triggers?.HasPendingIncident == true)
            {
                var incident = RuntimeDebugger.Triggers.RetrieveIncident();
                if (incident != null)
                {
                    string path = IncidentExporter.Export(incident);
                    Debug.Log($"[RuntimeDebugger] Auto-exported incident: {path}");
                }
            }
        }

        private void ShowTab(string tabName)
        {
            _currentTab = tabName;
            if (_tabContainer == null) return;
            _tabContainer.Clear();

            switch (tabName)
            {
                case "timeline": ShowTimelineTab(); break;
                case "metrics": ShowMetricsTab(); break;
                case "incident": ShowIncidentTab(); break;
            }
        }

        private void ShowTimelineTab()
        {
            if (!RuntimeDebugger.IsInitialized)
            {
                _tabContainer.Add(new Label(DebugLocale.Get("misc.notInitialized")));
                return;
            }

            var traces = RuntimeDebugger.GetTraceTree();
            var events = RuntimeDebugger.GetEvents();

            var info = new Label($"{DebugLocale.Get("timeline.traces")}: {traces.Length}  |  {DebugLocale.Get("timeline.events")}: {events.Length}");
            info.style.paddingBottom = 8;
            _tabContainer.Add(info);

            if (traces.Length == 0)
            {
                _tabContainer.Add(new Label(DebugLocale.Get("timeline.noData")));
                return;
            }

            var scroll = new ScrollView();
            foreach (var node in traces)
            {
                string name = RuntimeDebugger.GetEventName(node.EventHash);
                string indent = new string(' ', node.IsRoot ? 0 : 2);
                string duration = node.IsFinished ? $"{node.DurationMs}ms" : DebugLocale.Get("timeline.active");
                string color = node.DurationMs > 20 ? "#ff6b6b" : node.DurationMs > 5 ? "#ffd93d" : "#6bcf7f";

                var entry = new Label($"{indent}{name}  [F{node.Frame}]  {duration}");
                entry.style.color = ColorUtility.TryParseHtmlString(color, out var c) ? c : Color.white;
                entry.style.paddingLeft = node.IsRoot ? 0 : 16;
                entry.style.paddingTop = 2;
                entry.style.paddingBottom = 2;
                scroll.Add(entry);
            }
            _tabContainer.Add(scroll);
        }

        private void ShowMetricsTab()
        {
            if (!RuntimeDebugger.IsInitialized || RuntimeDebugger.Performance == null)
            {
                _tabContainer.Add(new Label(DebugLocale.Get("misc.notInitialized")));
                return;
            }

            var info = new Label($"{DebugLocale.Get("metrics.samples")}: {RuntimeDebugger.Performance.SampleCount}  |  {DebugLocale.Get("metrics.metrics")}: {RuntimeDebugger.Performance.MetricCount}");
            info.style.paddingBottom = 8;
            _tabContainer.Add(info);

            var defs = RuntimeDebugger.Performance.Definitions;
            var scroll = new ScrollView();

            foreach (var def in defs)
            {
                double value = RuntimeDebugger.Performance.GetLatestValue(def.Id);
                string name = DebugLocale.GetMetricName(def.Id);
                string display = def.Unit == MetricUnit.Bytes
                    ? FormatBytes((long)value)
                    : $"{value:F2} {DebugLocale.Get("unit.ms")}";

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 2;
                row.style.paddingBottom = 2;

                var nameLbl = new Label(name) { style = { width = 150, unityFontStyleAndWeight = FontStyle.Bold } };
                var valueLbl = new Label(display);
                row.Add(nameLbl);
                row.Add(valueLbl);
                scroll.Add(row);
            }
            _tabContainer.Add(scroll);
        }

        private void ShowIncidentTab()
        {
            if (!RuntimeDebugger.IsInitialized)
            {
                _tabContainer.Add(new Label(DebugLocale.Get("misc.notInitialized")));
                return;
            }

            var btn = new Button(OnCaptureIncident) { text = DebugLocale.Get("btn.captureNow") };
            btn.style.fontSize = 14;
            btn.style.height = 36;
            btn.style.marginBottom = 8;
            _tabContainer.Add(btn);

            var triggerInfo = new Label($"{DebugLocale.Get("incident.triggerState")}: {RuntimeDebugger.Triggers?.CurrentState}");
            _tabContainer.Add(triggerInfo);

            _tabContainer.Add(new VisualElement { style = { height = 8 } });

            _tabContainer.Add(new Label(DebugLocale.Get("incident.recentData")) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            _tabContainer.Add(new Label($"  {DebugLocale.Get("tab.timeline")}: {RuntimeDebugger.TraceCount}"));
            _tabContainer.Add(new Label($"  {DebugLocale.Get("timeline.events")}: {RuntimeDebugger.EventCount}"));
            _tabContainer.Add(new Label($"  {DebugLocale.Get("incident.lifecycle")}: {RuntimeDebugger.Lifecycle?.RecordCount ?? 0}"));
            _tabContainer.Add(new Label($"  {DebugLocale.Get("incident.async")}: {RuntimeDebugger.Async?.CompletedCount ?? 0}"));
            _tabContainer.Add(new Label($"  {DebugLocale.Get("incident.resources")}: {RuntimeDebugger.Resource?.RecordCount ?? 0}"));
            _tabContainer.Add(new Label($"  {DebugLocale.Get("incident.metricsLabel")}: {RuntimeDebugger.Performance?.SampleCount ?? 0}"));
        }

        private void OnInitialize()
        {
            if (!RuntimeDebugger.IsInitialized)
                RuntimeDebugger.Initialize();
            UpdateStatus();
        }

        private void OnStartSession(bool deepDebug, bool triggerDebug, int duration)
        {
            if (!RuntimeDebugger.IsInitialized)
                RuntimeDebugger.Initialize();

            if (deepDebug)
                RuntimeDebugger.StartSession(DebugMode.DeepDebug, duration);
            else if (triggerDebug)
                RuntimeDebugger.StartSession(DebugMode.TriggerDebug);
            else
                RuntimeDebugger.StartSession(DebugMode.Performance);

            UpdateStatus();
        }

        private void OnStopSession()
        {
            RuntimeDebugger.StopSession();
            UpdateStatus();
        }

        private void OnCaptureIncident()
        {
            if (!RuntimeDebugger.IsInitialized)
            {
                Debug.LogWarning("[RuntimeDebugger] Not initialized.");
                return;
            }

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.Custom,
                DebugLocale.Language == DebugLanguage.Chinese
                    ? "从编辑器窗口手动捕获"
                    : "Manual capture from Editor Window");

            string path = IncidentExporter.Export(incident);
            Debug.Log($"[RuntimeDebugger] Incident exported to: {path}");
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;

            if (RuntimeDebugger.IsInitialized)
            {
                string modeLabel = DebugLocale.Language == DebugLanguage.Chinese
                    ? RuntimeDebugger.Mode.ToString()
                    : RuntimeDebugger.Mode.ToString();
                _statusLabel.text = $"{DebugLocale.Get("status.ready")} | Mode: {RuntimeDebugger.Mode} | {DebugLocale.Get("timeline.events")}: {RuntimeDebugger.EventCount} | {DebugLocale.Get("timeline.traces")}: {RuntimeDebugger.TraceCount}";
                _statusLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
            }
            else
            {
                _statusLabel.text = DebugLocale.Get("status.notInitialized");
                _statusLabel.style.color = Color.red;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1048576.0:F2} MB";
        }
    }
}
