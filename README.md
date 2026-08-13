# Unity Runtime Debugger

<div align="center">

**Unity 客户端运行时调试与 AI 辅助诊断工具**

Low-overhead runtime observability & deep debugging infrastructure for Unity.

[![Unity](https://img.shields.io/badge/Unity-6000.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Tests: 116](https://img.shields.io/badge/Tests-116%20pass-brightgreen.svg)](#测试)

</div>

---

## 这是什么

一个可嵌入 Unity 项目的运行时调试基础设施。正常运行时保持极低开销（< 0.5ms/frame），当开发者主动进入深度调试或系统检测到异常时，临时提高采集粒度，记录完整的业务事件、状态变化、对象生命周期、异步操作和资源操作，形成结构化 Incident Bundle，交给 AI 进行分析。

> **核心理念：运行时保持便宜，定位问题时允许昂贵。**

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
- 在 Player Settings → Scripting Define Symbols 中添加 `RUNTIME_DEBUGGER_ENABLED` 以激活所有 API

## 快速上手

```csharp
using RuntimeDebugger;

// 1. 初始化（启动时调用一次）
RuntimeDebugger.Initialize();

// 2. 记录业务事件
RuntimeDebugger.RecordEvent("Turn.End");

// 3. 追踪执行链（支持嵌套）
using (RuntimeDebugger.Trace("Turn.End"))
{
    EndTurn();
    using (RuntimeDebugger.Trace("Event.Resolve"))
    {
        ResolveEvents();
    }
}

// 4. 追踪对象生命周期
int objId = RuntimeDebugger.Lifecycle.Track(this);

// 5. 追踪异步任务
int taskId = RuntimeDebugger.Async.StartTask("Addressables.LoadAssetAsync", objId);
// ... 异步完成后 ...
RuntimeDebugger.Async.Complete(taskId, Time.frameCount, TimeUtil.NowMs());

// 6. 追踪资源加载
RuntimeDebugger.Resource.RecordLoadStart("Assets/Sprites/icon.png", objId);

// 7. 手动捕获 Incident 并导出
var incident = IncidentBuilder.BuildFromCurrentState(
    IncidentType.Custom, "Manual capture");
IncidentExporter.Export(incident);

// 8. 打开 Editor 窗口
// Window → Analysis → Runtime Debugger
```

## 三种运行模式

| 模式 | 用途 | 开销 | 采集频率 |
|------|------|------|---------|
| **Performance** | 性能分析，使用 Unity 原生 Profiler API | < 0.2ms/frame | 每 10 帧采样 |
| **Deep Debug** | 手动开启，在时间窗口内获取完整逻辑证据 | 不限制 | 每帧详细采集 |
| **Trigger Debug** | 日常运行，触发后自动冻结+深度采集 | < 0.5ms/frame | 轻量 Ring Buffer |

### Trigger Debug 工作流

```
正常运行（Ring Buffer 保留最近 N 秒数据）
         ↓
    触发条件命中（Exception / 帧率尖峰 / GC 尖峰 / 自定义）
         ↓
    冻结缓冲（保存异常前数据）
         ↓
    深度采集 M 秒（高详细度）
         ↓
    生成 Incident Bundle
         ↓
    AI 分析 / 开发者验证
```

## Incident Bundle 输出

每次捕获生成一个完整的目录：

```
Incident_{type}_{timestamp}/
├── incident.json      摘要（类型 / 帧号 / 各数据计数）
├── timeline.json       追踪树节点（语义执行链）
├── events.json         运行时事件
├── lifecycle.json      对象生命周期记录（Create→Enable→Disable→Destroy）
├── async.json          异步任务记录（含竞态检测标记）
├── resources.json      资源操作记录（加载/释放）
├── states.json         状态快照
├── metrics.json        性能指标采样（FrameTime / GC / DrawCalls ...）
└── metadata.json       环境信息（Unity 版本 / 场景 / 时间）
```

## 架构

```
                         Unity Runtime
                              │
                ┌─────────────┴─────────────┐
                │                           │
        Lightweight Layer             Deep Debug Layer
                │                           │
        ProfilerRecorder              Event Trace
        ProfilerMarker                State Snapshot
        Frame Metrics                 Object Lifecycle
        GC / Memory                   Async Trace
        Rendering Metrics             Resource Trace
                │                           │
                └─────────────┬─────────────┘
                              ↓
                         Ring Buffer
                              │
                  ┌───────────┴───────────┐
                  │                       │
             Normal Runtime          Incident Trigger
                                          │
                              ┌───────────┴───────────┐
                              ↓                       ↓
                       Freeze Pre-Trace        Deep Capture
                              │                       │
                              └───────────┬───────────┘
                                          ↓
                                  Incident Bundle
                                          │
                                  Existing AI / LLM
                                          ↓
                                  Diagnosis / Hypothesis
```

## 功能模块

### 语义追踪 (Semantic Trace)

不追踪所有底层方法调用，只追踪具有业务意义的执行链：

```
Turn.End
  └── Event.Resolve
        ├── Technology.Unlock
        └── UI.Refresh
              └── Resource.Load ×18
```

### 对象生命周期追踪 (Lifecycle Tracking)

检测异步回调访问已销毁对象（竞态条件）：

```
OperatorPanel#1921
  ├── Create [F30]
  ├── Enable  [F30]
  ├── Async Load Start [F31]      ← 启动异步加载
  ├── Disable [F33]
  ├── Destroy [F34]               ← 对象已销毁
  └── Async Load Complete [F36]   ← ⚠ 竞态！回调访问已销毁对象
```

### 异步任务追踪 (Async Trace)

记录每个异步任务的完整生命周期，自动检测 Owner 销毁前未完成：

```
Task #192
  Owner:     OperatorPanel#1921
  Operation: Addressables.LoadAssetAsync
  Start:     12.31s [F31]
  Owner Destroyed: 12.53s [F34]    ← ⚠ Owner 在完成前被销毁
  Complete:  12.58s [F36]
```

### 资源追踪 (Resource Trace)

追踪资源加载/释放操作，自动检测重复加载：

```
LoadStart   "Assets/Sprites/TechIcon.png"  [F31]  Owner:Panel#1921
LoadComplete "Assets/Sprites/TechIcon.png" [F36]
LoadStart   "Assets/Sprites/TechIcon.png"  [F38]  ← ⚠ 重复加载
```

### AI 分析 (AI Integration)

构建结构化 Prompt，将 Incident Bundle + 运行时证据提交给 LLM：

```
输入: Incident Bundle + Relevant Code + Profiler Data
输出:
  1. Incident Summary
  2. Evidence (引用具体追踪节点/指标)
  3. Candidate Root Causes (标注置信度)
  4. Evidence Chain
  5. Unknowns
  6. Suggested Verification Experiments
```

> 没有 Runtime Evidence 支持的内容标记为 Hypothesis，而非 Root Cause。

## 中英双语

Editor 窗口支持中文/英文切换，所有 UI 文本、指标名称、生命周期阶段、异步状态、资源操作、AI Prompt 均已本地化。

```csharp
// 通过代码切换语言
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
- **ProfilerMarker 集成**：Trace 同时在 Unity Profiler 中可见

## 开发阶段

- [x] M1 — Runtime Core（RingBuffer / TraceTree / IncidentBuilder / Exporter）
- [x] M2 — State / Lifecycle / Async / Resource Tracking
- [x] M3 — Performance Mode（ProfilerRecorder）
- [x] M4 — Trigger System（自动异常检测 + 深度采集）
- [x] M5 — Editor Window（UI Toolkit）
- [x] M6 — AI Integration（ContextBuilder / PromptBuilder / AIResult）
- [ ] M7 — 真实项目验证

## License

[MIT](Assets/RuntimeDebugger/LICENSE.md)
