# Unity Runtime Debugger

<div align="center">

**Unity 客户端运行时调试与 AI 辅助诊断工具**

导入即用 · 零代码改动 · 自动监测 · AI 辅助分析

[![Unity](https://img.shields.io/badge/Unity-6000.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](Assets/RuntimeDebugger/LICENSE.md)
[![Tests: 116](https://img.shields.io/badge/Tests-116%20pass-brightgreen.svg)](#测试)

</div>

---

## 这是什么

一个可嵌入 Unity 项目的运行时调试基础设施。**导入包后不需要改任何游戏代码**，工具自动监测运行状态，出 Bug 时自动捕获完整上下文，生成结构化报告供 AI 分析。

> **核心理念：导入即用，零代码改动，异常自动追溯。**

## 安装

### 通过 Git URL 安装（推荐）

Unity Editor → `Window` → `Package Manager` → `+` → `Add package from git URL`

```
https://github.com/CrimeyMike/Unity_Runtime_Debugger.git?path=/Assets/RuntimeDebugger
```

或手动编辑 `Packages/manifest.json`：

```json
{
    "dependencies": {
        "com.runtimedebugger.core": "https://github.com/CrimeyMike/Unity_Runtime_Debugger.git?path=/Assets/RuntimeDebugger"
    }
}
```

### 环境要求

- Unity 2022.3+（已测试 Unity 6000.3.18f1）
- 在 Player Settings → Scripting Define Symbols 中添加 `RUNTIME_DEBUGGER_ENABLED`

## 使用方法（3 步，不改游戏代码）

### 第 1 步：导入包

按上述 Git URL 方式导入。

### 第 2 步：添加编译符号

`Edit → Project Settings → Player → Other → Scripting Define Symbols`

输入 `RUNTIME_DEBUGGER_ENABLED`，回车。

### 第 3 步：运行游戏

直接点 ▶ Play。

工具会通过 `[RuntimeInitializeOnLoadMethod]` 自动完成一切：
- 自动初始化调试器
- 自动扫描场景中所有对象的生命周期（每 0.2 秒）
- 自动采集性能指标（帧率/GC/DrawCalls，每帧）
- 自动捕获所有 `Debug.Log` 调用
- 自动注册异常/帧率尖峰/GC 飙升触发器
- 出 Bug 时自动冻结 + 深度采集 + 导出报告

**不需要挂组件，不需要改代码，不需要手动 Initialize。**

## Editor 窗口

`Window → Analysis → Runtime Debugger`

双栏布局，所有调试信息直接在窗口内查看，不需要去看 JSON 文件：

```
┌──────────────────────────────────────────────────────────────┐
│ ● Ready | Mode: TriggerDebug        [EN][中文][Init]          │
├──────────────────────────────────────────────────────────────┤
│ [TriggerDebug ▼] Duration: [5] [▶Start] [⬛Stop] [📷Capture]  │
│ Events: 42 | Traces: 12 | Objects: 8 | Async: 2 | Metrics: 240│
├────────────┬─────────────────────────────────────────────────┤
│ SIDEBAR    │  DETAIL PANEL                                   │
│            │                                                 │
│ 📊 Overview│  Overview: stat cards + metric bars + warnings  │
│ 🕐 Timeline│  Timeline: expandable tree with duration bars   │
│ 📋 Events  │  Events: color-coded log (error/warn/create/..) │
│ 📈 Metrics │  Metrics: latest values + expandable history     │
│ 🔄 Lifecycle│ Lifecycle: per-object Create→Destroy chain      │
│ ⚡ Async   │  Async: race conditions highlighted              │
│ 📦 Resources│ Resources: duplicate loads warned              │
│ 🚨 Incidents│ Incidents: export + AI prompt generation       │
└────────────┴─────────────────────────────────────────────────┘
```

- 运行时每 0.2 秒自动刷新
- 左侧分类显示实时数据量，点击切换
- 右侧详情面板可展开/折叠
- Timeline 树带耗时条（🟢≤5ms / 🟡5-20ms / 🔴>20ms）
- 竞态条件和重复加载用 ⚠ 高亮警告
- Incidents 页可一键生成 AI Prompt（复制到剪贴板）

## 零代码自动监测能力

| 能力 | 怎么工作 | 需要改代码？ |
|------|---------|:-----------:|
| 场景对象追踪 | 每 0.2s 扫描场景，自动检测所有 MonoBehaviour 的创建/启用/禁用/销毁 | ❌ |
| Debug.Log 捕获 | Hook `Application.logMessageReceived`，现有日志自动进入 Ring Buffer | ❌ |
| 异常自动检测 | 游戏抛异常时自动触发 Incident | ❌ |
| 帧率自动监控 | 帧时间 > 33ms 自动触发 | ❌ |
| GC 自动监控 | 单帧 GC > 1MB 自动触发 | ❌ |
| 性能指标自动采集 | ProfilerRecorder 每帧采样 8 个指标 | ❌ |
| 自动冻结+导出 | 触发后自动冻结 → 深度采集 → 导出 9 个 JSON | ❌ |

## 可选增强（手动 API）

零代码已覆盖大部分调试需求。如果需要更深的业务语义追踪，可以手动埋点：

```csharp
using RuntimeDebugger;

// 语义追踪（建立业务因果链）
using (RuntimeDebugger.Trace("Turn.End"))
{
    EndTurn();
    using (RuntimeDebugger.Trace("Event.Resolve"))
    {
        ResolveEvents();
    }
}

// 记录业务事件
RuntimeDebugger.RecordEvent("Turn.End");

// 对象生命周期追踪
int objId = RuntimeDebugger.Lifecycle.Track(this);

// 异步任务追踪
int taskId = RuntimeDebugger.Async.StartTask("LoadAsset", objId);
RuntimeDebugger.Async.Complete(taskId, Time.frameCount, TimeUtil.NowMs());

// 资源加载追踪
RuntimeDebugger.Resource.RecordLoadStart("Assets/icon.png", objId);

// 手动捕获 Incident
var incident = IncidentBuilder.BuildFromCurrentState(
    IncidentType.Custom, "Manual capture");
IncidentExporter.Export(incident);
```

或者让 MonoBehaviour 继承 `DebuggableBehaviour`，自动获得生命周期追踪（不用写 Track 调用）：

```csharp
// 改一个词即可
public class MyPanel : DebuggableBehaviour { ... }
```

## 三种运行模式

| 模式 | 用途 | 开销 | 采集频率 |
|------|------|------|---------|
| **Trigger Debug**（默认） | 日常运行，触发后自动冻结+深度采集 | < 0.5ms/frame | 轻量 Ring Buffer |
| **Performance** | 性能分析，使用 Unity 原生 Profiler API | < 0.2ms/frame | 每 10 帧采样 |
| **Deep Debug** | 手动开启，在时间窗口内获取完整逻辑证据 | 不限制 | 每帧详细采集 |

## Incident Bundle 输出

每次捕获生成一个完整的目录：

```
Incident_{type}_{timestamp}/
├── incident.json      摘要（类型 / 帧号 / 各数据计数）
├── timeline.json      追踪树节点（语义执行链）
├── events.json        运行时事件
├── lifecycle.json     对象生命周期记录（Create→Enable→Disable→Destroy）
├── async.json         异步任务记录（含竞态检测标记）
├── resources.json     资源操作记录（加载/释放）
├── states.json        状态快照
├── metrics.json       性能指标采样（FrameTime / GC / DrawCalls ...）
└── metadata.json      环境信息（Unity 版本 / 场景 / 时间）
```

导出路径：`Application.persistentDataPath/Incidents/`

## 架构

```
Unity Runtime
    ↓
[RuntimeInitializeOnLoadMethod] 自动初始化
    ↓
┌─────────────────────────────────────────────┐
│ 零代码自动监测层                              │
│  AutoSceneMonitor — 场景对象自动扫描         │
│  AutoInstrumentation — Debug.Log 自动捕获    │
│  PerformanceMonitor — ProfilerRecorder 采样  │
│  TriggerSystem — 异常/帧率/GC 自动触发        │
└─────────────────────────────────────────────┘
    ↓
Ring Buffer（保留最近 N 秒数据）
    ↓
触发条件命中 → 冻结 → 深度采集 → Incident Bundle
    ↓
AI 分析 / 开发者在窗口内查看
```

## AI 分析

在 Editor 窗口 Incidents 页点击「🤖 Generate AI Prompt」，自动：
1. 捕获当前运行时状态
2. 构建结构化分析 Prompt
3. 复制到剪贴板
4. 粘贴到 Claude / ChatGPT 即可获得诊断

```
输入: Incident Bundle + Runtime Context
输出:
  1. Summary（摘要）
  2. Evidence（证据，引用具体追踪节点/指标）
  3. Hypotheses（假设，标注置信度）
  4. Unknowns（未知项）
  5. Verification（建议的验证步骤）
```

> 没有 Runtime Evidence 支持的内容标记为 Hypothesis，而非 Root Cause。

## 中英双语

Editor 窗口支持中文/英文切换，所有 UI 文本、指标名称、生命周期阶段、异步状态、资源操作、AI Prompt 均已本地化。

```csharp
// 通过代码切换
DebugLocale.SetLanguage(DebugLanguage.Chinese);  // 中文
DebugLocale.SetLanguage(DebugLanguage.English);   // English
```

## API 速查

| 类 | 用途 |
|---|------|
| `RuntimeDebugger` | 静态门面 — `Initialize()` / `RecordEvent()` / `Trace()` / `StartSession()` |
| `RuntimeDebugger.Performance` | ProfilerRecorder 性能指标采集 |
| `RuntimeDebugger.Lifecycle` | 对象生命周期追踪 + 竞态检测 |
| `RuntimeDebugger.Async` | 异步任务追踪 + Owner 销毁检测 |
| `RuntimeDebugger.Resource` | 资源加载/释放追踪 + 重复加载检测 |
| `RuntimeDebugger.Triggers` | 自动 Incident 触发系统 |
| `AutoSceneMonitor` | 零代码场景对象自动监测 |
| `AutoInstrumentation` | 零代码 Debug.Log 自动捕获 + 触发器自动注册 |
| `DebuggableBehaviour` | 继承即自动追踪生命周期（可选） |
| `IncidentBuilder` | 从冻结缓冲区组装 Incident |
| `IncidentExporter` | 导出 Incident 为 JSON Bundle |
| `AIPromptBuilder` | 构建 LLM 分析 Prompt |
| `AIResult` | 解析 LLM 输出为结构化结果 |
| `DebugLocale` | 中英双语本地化 |

## 测试

116 个单元测试覆盖所有核心模块：

| 测试 | 数量 | 覆盖范围 |
|------|------|---------|
| RingBufferTests | 12 | 写入/覆盖/冻结/范围查询 |
| TraceTreeTests | 8 | 嵌套追踪/父子关系/冻结 |
| EventSystemTests | 13 | 事件记录/ContextId 关联/完整管道 |
| IncidentBuilderTests | 7 | Incident 构建/导出/JSON 验证 |
| StateSnapshotTests | 8 | 状态注册/采集/冻结 |
| LifecycleTrackerTests | 10 | 全生命周期链/竞态检测 |
| AsyncTrackerTests | 11 | 异步任务/竞态检测/多任务 |
| ResourceTrackerTests | 11 | 加载/释放/重复检测 |
| PerformanceMonitorTests | 12 | ProfilerRecorder 采样/频率 |
| TriggerSystemTests | 11 | 状态机/手动触发/自动触发 |
| AIPromptBuilderTests | 9 | 上下文构建/Prompt 生成/LLM 解析 |

运行测试：Unity Editor → 菜单 `Runtime Debugger → Run Edit Mode Tests`

## 技术设计

- **零 GC 运行时**：struct 数据结构 + 预分配数组 + int hash 代替 string
- **环形缓冲区**：固定容量，O(1) 写入/覆盖，支持冻结
- **条件编译**：`[Conditional("RUNTIME_DEBUGGER_ENABLED")]` — 未定义时零开销
- **零代码自动初始化**：`[RuntimeInitializeOnLoadMethod]` — 游戏启动即生效
- **ProfilerMarker 集成**：Trace 同时在 Unity Profiler 中可见

## 开发阶段

- [x] M1 — Runtime Core（RingBuffer / TraceTree / IncidentBuilder / Exporter）
- [x] M2 — State / Lifecycle / Async / Resource Tracking
- [x] M3 — Performance Mode（ProfilerRecorder）
- [x] M4 — Trigger System（自动异常检测 + 深度采集）
- [x] M5 — Editor Window（双栏布局，所有信息窗口内查看）
- [x] M6 — AI Integration（ContextBuilder / PromptBuilder / AIResult）
- [x] 零代码自动监测（AutoSceneMonitor / AutoInstrumentation）
- [ ] M7 — 真实项目验证

## License

[MIT](Assets/RuntimeDebugger/LICENSE.md)
