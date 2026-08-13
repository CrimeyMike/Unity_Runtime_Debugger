# Unity Runtime Debugger

**Low-overhead runtime observability & deep debugging infrastructure for Unity.**

运行时保持低开销，定位问题时允许深度采集。捕获业务事件、状态变化、对象生命周期、异步操作、资源操作，形成结构化 Incident Bundle，供 AI 辅助分析。

## Features

- 🎯 **Semantic Trace** — `using (RuntimeDebugger.Trace("Turn.End"))` 追踪业务执行链
- 📊 **Performance Monitor** — ProfilerRecorder 采集 FrameTime/GC/DrawCalls 等指标
- 🔄 **Object Lifecycle** — 检测异步回调访问已销毁对象（竞态条件）
- ⚡ **Async Trace** — 追踪异步任务生命周期，检测 Owner 销毁前未完成
- 📦 **Resource Trace** — 追踪资源加载/释放，检测重复加载
- 🚨 **Trigger System** — 自动检测异常 → 冻结缓冲 → 深度采集 → 生成 Incident
- 🤖 **AI Integration** — 构建 LLM 分析 Prompt，解析诊断结果
- 🌐 **Bilingual** — 中英双语 UI / 指标名 / AI Prompt

## Installation

### Via Git URL (UPM)

在目标 Unity 项目的 `Packages/manifest.json` 中添加：

```json
{
    "dependencies": {
        "com.runtimedebugger.core": "https://github.com/CrimeyMike/Unity_Runtime_Debugger.git?path=/Assets/RuntimeDebugger"
    }
}
```

或在 Unity Editor 中：`Window → Package Manager → + → Add package from git URL`，输入：

```
https://github.com/CrimeyMike/Unity_Runtime_Debugger.git?path=/Assets/RuntimeDebugger
```

### Requirements

- Unity 2022.3+ (tested on Unity 6000.3.18f1)
- URP (optional, for rendering metrics)

## Quick Start

```csharp
using RuntimeDebugger;

// 1. Initialize (once at startup)
RuntimeDebugger.Initialize();

// 2. Record events
RuntimeDebugger.RecordEvent("Turn.End");

// 3. Trace execution
using (RuntimeDebugger.Trace("Turn.End"))
{
    EndTurn();
    // Nested traces supported
    using (RuntimeDebugger.Trace("Event.Resolve"))
    {
        ResolveEvents();
    }
}

// 4. Track object lifecycle
int objId = RuntimeDebugger.Lifecycle.Track(this);

// 5. Track async operations
int taskId = RuntimeDebugger.Async.StartTask("LoadAsset", objId);

// 6. Manual incident capture
var incident = IncidentBuilder.BuildFromCurrentState(
    IncidentType.Custom, "My capture");
IncidentExporter.Export(incident);

// 7. Open the Editor Window
// Window → Analysis → Runtime Debugger
```

## Architecture

```
Unity Runtime
    ↓
Lightweight Layer          Deep Debug Layer
  ProfilerRecorder           Event Trace
  ProfilerMarker             State Snapshot
  Frame Metrics              Object Lifecycle
  GC / Memory               Async Trace
  Rendering Metrics          Resource Trace
    ↓                           ↓
         Ring Buffer
              ↓
     Incident Trigger
              ↓
     Freeze + Deep Capture
              ↓
     Incident Bundle (9 JSON files)
              ↓
     AI / LLM Analysis
```

## Incident Bundle Output

```
Incident_{type}_{timestamp}/
├── incident.json      Summary (type, frame, counts)
├── timeline.json       Trace tree nodes
├── events.json         Runtime events
├── lifecycle.json      Object lifecycle records
├── async.json          Async task records
├── resources.json      Resource operations
├── states.json         State snapshots
├── metrics.json        Performance samples
└── metadata.json       Environment info
```

## API Reference

| Class | Purpose |
|-------|---------|
| `RuntimeDebugger` | Static facade — `Initialize()`, `RecordEvent()`, `Trace()`, `StartSession()` |
| `RuntimeDebugger.Performance` | `ProfilerRecorder` metrics sampling |
| `RuntimeDebugger.Lifecycle` | Object lifecycle tracking + race condition detection |
| `RuntimeDebugger.Async` | Async task tracking + owner-destroyed detection |
| `RuntimeDebugger.Resource` | Resource load/release tracking + duplicate detection |
| `RuntimeDebugger.Triggers` | Automatic incident trigger system |
| `IncidentBuilder` | Assemble incidents from frozen buffers |
| `IncidentExporter` | Export incidents to JSON |
| `AIPromptBuilder` | Build LLM analysis prompts |
| `DebugLocale` | Chinese/English localization |

## License

MIT
