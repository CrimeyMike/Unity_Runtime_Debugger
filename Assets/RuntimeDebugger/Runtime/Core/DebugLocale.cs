using System.Collections.Generic;

namespace RuntimeDebugger
{
    public enum DebugLanguage
    {
        English,
        Chinese
    }

    /// <summary>
    /// Lightweight localization system for Runtime Debugger UI and exports.
    /// No external dependencies — pure string lookup.
    /// </summary>
    public static class DebugLocale
    {
        private static DebugLanguage s_language = DebugLanguage.English;

        public static DebugLanguage Language
        {
            get => s_language;
            set => s_language = value;
        }

        public static void SetLanguage(DebugLanguage lang) => s_language = lang;
        public static void SetLanguage(string langCode)
        {
            s_language = langCode == "zh" || langCode == "zh-CN" || langCode == "Chinese"
                ? DebugLanguage.Chinese
                : DebugLanguage.English;
        }

        // ── Localization keys ──────────────────────────────────

        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // Window
            ["window.title"] = "Runtime Debugger",
            ["status.notInitialized"] = "Not Initialized",
            ["status.ready"] = "Ready",
            ["btn.initialize"] = "Initialize",
            ["btn.start"] = "▶ Start",
            ["btn.stop"] = "⬛ Stop",
            ["btn.capture"] = "📷 Capture",
            ["btn.captureNow"] = "📷 Capture Incident Now",

            // Mode
            ["mode.label"] = "Mode:",
            ["mode.performance"] = "Performance",
            ["mode.deepDebug"] = "Deep Debug",
            ["mode.triggerDebug"] = "Trigger Debug",

            // Fields
            ["duration.label"] = "Duration (s): ",

            // Tabs
            ["tab.timeline"] = "Timeline",
            ["tab.metrics"] = "Metrics",
            ["tab.incident"] = "Incident",

            // Timeline tab
            ["timeline.traces"] = "Traces",
            ["timeline.events"] = "Events",
            ["timeline.noData"] = "No trace data. Run the game with tracing enabled.",
            ["timeline.active"] = "active",

            // Metrics tab
            ["metrics.samples"] = "Samples",
            ["metrics.metrics"] = "Metrics",

            // Incident tab
            ["incident.triggerState"] = "Trigger State",
            ["incident.recentData"] = "Recent Data:",
            ["incident.lifecycle"] = "Lifecycle",
            ["incident.async"] = "Async",
            ["incident.resources"] = "Resources",
            ["incident.metricsLabel"] = "Metrics",

            // Metrics names
            ["metric.FrameTime"] = "Frame Time",
            ["metric.CPUTime"] = "CPU Time",
            ["metric.GPUTime"] = "GPU Time",
            ["metric.GCAlloc"] = "GC Alloc",
            ["metric.ManagedMemory"] = "Managed Memory",
            ["metric.DrawCalls"] = "Draw Calls",
            ["metric.SetPass"] = "SetPass",
            ["metric.Batches"] = "Batches",

            // Incident context sections
            ["section.incident"] = "=== INCIDENT ===",
            ["section.timeline"] = "=== TIMELINE ===",
            ["section.metrics"] = "=== METRICS (latest values) ===",
            ["section.lifecycle"] = "=== LIFECYCLE ===",
            ["section.async"] = "=== ASYNC TRACE ===",
            ["section.resources"] = "=== RESOURCE OPERATIONS ===",
            ["section.events"] = "=== EVENTS ===",

            // Lifecycle phases
            ["lifecycle.Create"] = "Create",
            ["lifecycle.Enable"] = "Enable",
            ["lifecycle.Disable"] = "Disable",
            ["lifecycle.Destroy"] = "Destroy",

            // Async
            ["async.raceCondition"] = "⚠ RACE CONDITION",
            ["async.ownerDestroyed"] = "Owner destroyed at frame",

            // Units
            ["unit.ms"] = "ms",
            ["unit.bytes"] = "bytes",
            ["unit.count"] = "",

            // AI Prompt
            ["ai.intro"] = "You are analyzing a Unity runtime incident captured by the Unity Runtime Debugger.",
            ["ai.task"] = "Your task: analyze the root cause using the provided runtime evidence.",
            ["ai.summary"] = "Summary",
            ["ai.evidence"] = "Evidence",
            ["ai.hypotheses"] = "Hypotheses",
            ["ai.unknowns"] = "Unknowns",
            ["ai.verification"] = "Verification",

            // Misc
            ["misc.moreNodes"] = "more trace nodes",
            ["misc.moreEvents"] = "more events",
            ["misc.noIncidentData"] = "No incident data.",
            ["misc.notInitialized"] = "Initialize first.",
            ["misc.language"] = "Language",
        };

        public static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
        {
            // Window
            ["window.title"] = "运行时调试器",
            ["status.notInitialized"] = "未初始化",
            ["status.ready"] = "就绪",
            ["btn.initialize"] = "初始化",
            ["btn.start"] = "▶ 开始",
            ["btn.stop"] = "⬛ 停止",
            ["btn.capture"] = "📷 捕获",
            ["btn.captureNow"] = "📷 立即捕获事件",

            // Mode
            ["mode.label"] = "模式：",
            ["mode.performance"] = "性能模式",
            ["mode.deepDebug"] = "深度调试",
            ["mode.triggerDebug"] = "触发调试",

            // Fields
            ["duration.label"] = "时长（秒）：",

            // Tabs
            ["tab.timeline"] = "时间线",
            ["tab.metrics"] = "指标",
            ["tab.incident"] = "事件",

            // Timeline tab
            ["timeline.traces"] = "追踪",
            ["timeline.events"] = "事件",
            ["timeline.noData"] = "无追踪数据。请启用追踪后运行游戏。",
            ["timeline.active"] = "进行中",

            // Metrics tab
            ["metrics.samples"] = "采样数",
            ["metrics.metrics"] = "指标数",

            // Incident tab
            ["incident.triggerState"] = "触发器状态",
            ["incident.recentData"] = "近期数据：",
            ["incident.lifecycle"] = "生命周期",
            ["incident.async"] = "异步",
            ["incident.resources"] = "资源",
            ["incident.metricsLabel"] = "指标",

            // Metrics names
            ["metric.FrameTime"] = "帧时间",
            ["metric.CPUTime"] = "CPU 时间",
            ["metric.GPUTime"] = "GPU 时间",
            ["metric.GCAlloc"] = "GC 分配",
            ["metric.ManagedMemory"] = "托管内存",
            ["metric.DrawCalls"] = "Draw Call",
            ["metric.SetPass"] = "SetPass",
            ["metric.Batches"] = "批次",

            // Incident context sections
            ["section.incident"] = "=== 事件 ===",
            ["section.timeline"] = "=== 时间线 ===",
            ["section.metrics"] = "=== 指标（最新值）===",
            ["section.lifecycle"] = "=== 生命周期 ===",
            ["section.async"] = "=== 异步追踪 ===",
            ["section.resources"] = "=== 资源操作 ===",
            ["section.events"] = "=== 事件 ===",

            // Lifecycle phases
            ["lifecycle.Create"] = "创建",
            ["lifecycle.Enable"] = "启用",
            ["lifecycle.Disable"] = "禁用",
            ["lifecycle.Destroy"] = "销毁",

            // Async
            ["async.raceCondition"] = "⚠ 竞态条件",
            ["async.ownerDestroyed"] = "Owner 在帧",

            // Units
            ["unit.ms"] = "ms",
            ["unit.bytes"] = "字节",
            ["unit.count"] = "",

            // AI Prompt
            ["ai.intro"] = "你正在分析由 Unity Runtime Debugger 捕获的 Unity 运行时事件。",
            ["ai.task"] = "你的任务：基于提供的运行时证据分析根本原因。",
            ["ai.summary"] = "摘要",
            ["ai.evidence"] = "证据",
            ["ai.hypotheses"] = "假设",
            ["ai.unknowns"] = "未知项",
            ["ai.verification"] = "验证",

            // Misc
            ["misc.moreNodes"] = "个更多追踪节点",
            ["misc.moreEvents"] = "个更多事件",
            ["misc.noIncidentData"] = "无事件数据。",
            ["misc.notInitialized"] = "请先初始化。",
            ["misc.language"] = "语言",
        };

        /// <summary>Get localized string by key.</summary>
        public static string Get(string key)
        {
            var dict = s_language == DebugLanguage.Chinese ? Zh : En;
            return dict.TryGetValue(key, out var value) ? value : key;
        }

        /// <summary>Get localized metric name by metric ID.</summary>
        public static string GetMetricName(int metricId)
        {
            string key = metricId switch
            {
                MetricIds.FrameTime => "metric.FrameTime",
                MetricIds.CPUTime => "metric.CPUTime",
                MetricIds.GPUTime => "metric.GPUTime",
                MetricIds.GCAlloc => "metric.GCAlloc",
                MetricIds.ManagedMemory => "metric.ManagedMemory",
                MetricIds.DrawCalls => "metric.DrawCalls",
                MetricIds.SetPass => "metric.SetPass",
                MetricIds.Batches => "metric.Batches",
                _ => null
            };
            return key != null ? Get(key) : $"Metric_{metricId}";
        }

        /// <summary>Get localized lifecycle phase name.</summary>
        public static string GetPhaseName(LifecyclePhase phase)
        {
            return Get($"lifecycle.{phase}");
        }

        /// <summary>Get localized async status name.</summary>
        public static string GetAsyncStatus(AsyncStatus status)
        {
            return s_language == DebugLanguage.Chinese
                ? status switch
                {
                    AsyncStatus.Running => "运行中",
                    AsyncStatus.Completed => "已完成",
                    AsyncStatus.Cancelled => "已取消",
                    AsyncStatus.Failed => "失败",
                    _ => status.ToString()
                }
                : status.ToString();
        }

        /// <summary>Get localized resource operation name.</summary>
        public static string GetResourceOperation(ResourceOperation op)
        {
            return s_language == DebugLanguage.Chinese
                ? op switch
                {
                    ResourceOperation.LoadStart => "加载开始",
                    ResourceOperation.LoadComplete => "加载完成",
                    ResourceOperation.LoadFail => "加载失败",
                    ResourceOperation.Release => "释放",
                    _ => op.ToString()
                }
                : op.ToString();
        }
    }
}
