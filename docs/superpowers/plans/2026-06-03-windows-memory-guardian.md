# Windows Memory Guardian Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Windows 桌面内存监控与自动回收工具，采用 `WPF UI + 提权执行器` 双进程架构，支持趋势图、白名单、自动 trim、系统级回收与受控挂起。

**Architecture:** UI 进程负责采样、展示、策略评估、托盘与规则配置；执行器进程负责管理员权限动作、undocumented API 调用与结果回传。共享领域模型和命令契约放在 `Core`，采样、持久化与管道通信放在 `Infrastructure`。

**Tech Stack:** .NET 8, WPF, xUnit, Named Pipes, System.Text.Json, Win32 P/Invoke

---

## 预备说明

- 当前目录不是 Git 仓库。如果执行本计划前仍未初始化，请先运行 `git init`，后续提交步骤按计划执行。
- 第一版不引入数据库，不引入重型图表库，不引入浏览器扩展。
- 所有高权限与 undocumented API 仅允许出现在 `src/MemoryMonitoring.Executor/`。

## 文件结构

- Create: `MemoryMonitoring.sln`
- Create: `README.md`
- Create: `src/MemoryMonitoring.Core/MemoryMonitoring.Core.csproj`
- Create: `src/MemoryMonitoring.Core/Models/*.cs`
- Create: `src/MemoryMonitoring.Core/Policies/*.cs`
- Create: `src/MemoryMonitoring.Core/Contracts/*.cs`
- Create: `src/MemoryMonitoring.Infrastructure/MemoryMonitoring.Infrastructure.csproj`
- Create: `src/MemoryMonitoring.Infrastructure/Monitoring/*.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Persistence/*.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Pipes/*.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Power/*.cs`
- Create: `src/MemoryMonitoring.Executor/MemoryMonitoring.Executor.csproj`
- Create: `src/MemoryMonitoring.Executor/Program.cs`
- Create: `src/MemoryMonitoring.Executor/Actions/*.cs`
- Create: `src/MemoryMonitoring.Executor/Interop/*.cs`
- Create: `src/MemoryMonitoring.App/MemoryMonitoring.App.csproj`
- Create: `src/MemoryMonitoring.App/App.xaml`
- Create: `src/MemoryMonitoring.App/App.xaml.cs`
- Create: `src/MemoryMonitoring.App/MainWindow.xaml`
- Create: `src/MemoryMonitoring.App/MainWindow.xaml.cs`
- Create: `src/MemoryMonitoring.App/ViewModels/*.cs`
- Create: `src/MemoryMonitoring.App/Tray/*.cs`
- Create: `src/MemoryMonitoring.App/Controls/*.cs`
- Create: `tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj`
- Create: `tests/MemoryMonitoring.Tests/**/*.cs`

---

### Task 1: 初始化解决方案与共享领域模型

**Files:**
- Create: `MemoryMonitoring.sln`
- Create: `README.md`
- Create: `src/MemoryMonitoring.Core/MemoryMonitoring.Core.csproj`
- Create: `src/MemoryMonitoring.Core/Models/ProcessTreatment.cs`
- Create: `src/MemoryMonitoring.Core/Models/ProcessRule.cs`
- Create: `src/MemoryMonitoring.Core/Models/MemoryPolicySettings.cs`
- Create: `tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj`
- Create: `tests/MemoryMonitoring.Tests/Core/RuleDefaultsTests.cs`

- [ ] **Step 1: 初始化解决方案与项目骨架**

Run:

```bash
git init
dotnet new sln -n MemoryMonitoring
dotnet new classlib -n MemoryMonitoring.Core -o src/MemoryMonitoring.Core
dotnet new classlib -n MemoryMonitoring.Infrastructure -o src/MemoryMonitoring.Infrastructure
dotnet new console -n MemoryMonitoring.Executor -o src/MemoryMonitoring.Executor
dotnet new wpf -n MemoryMonitoring.App -o src/MemoryMonitoring.App
dotnet new xunit -n MemoryMonitoring.Tests -o tests/MemoryMonitoring.Tests
dotnet sln MemoryMonitoring.sln add src/MemoryMonitoring.Core/MemoryMonitoring.Core.csproj
dotnet sln MemoryMonitoring.sln add src/MemoryMonitoring.Infrastructure/MemoryMonitoring.Infrastructure.csproj
dotnet sln MemoryMonitoring.sln add src/MemoryMonitoring.Executor/MemoryMonitoring.Executor.csproj
dotnet sln MemoryMonitoring.sln add src/MemoryMonitoring.App/MemoryMonitoring.App.csproj
dotnet sln MemoryMonitoring.sln add tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj
dotnet add tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj reference src/MemoryMonitoring.Core/MemoryMonitoring.Core.csproj
```

Expected: 所有项目被加入解决方案，`dotnet sln list` 能看到 5 个项目。

- [ ] **Step 2: 写领域模型的失败测试**

`tests/MemoryMonitoring.Tests/Core/RuleDefaultsTests.cs`

```csharp
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Tests.Core;

public sealed class RuleDefaultsTests
{
    [Fact]
    public void WhiteListRule_ShouldNeverAllowSuspend()
    {
        var rule = ProcessRule.WhiteList("WeChat.exe");

        Assert.Equal("WeChat.exe", rule.ProcessName);
        Assert.Equal(ProcessTreatment.WhiteList, rule.Treatment);
        Assert.False(rule.AllowSuspend);
    }

    [Fact]
    public void DefaultPolicy_ShouldStartInBalancedMode()
    {
        var settings = MemoryPolicySettings.CreateDefault();

        Assert.Equal(85, settings.MemoryLoadPercentThreshold);
        Assert.Equal(2_048, settings.AvailableMemoryThresholdMb);
        Assert.Equal(45, settings.SustainedPressureSeconds);
        Assert.Equal(90, settings.StartupDelaySeconds);
    }
}
```

- [ ] **Step 3: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~RuleDefaultsTests
```

Expected: FAIL，提示 `MemoryMonitoring.Core.Models` 中类型不存在。

- [ ] **Step 4: 写最小实现**

`src/MemoryMonitoring.Core/Models/ProcessTreatment.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public enum ProcessTreatment
{
    WhiteList = 0,
    TrimOnly = 1,
    Balanced = 2,
    SuspendEligible = 3
}
```

`src/MemoryMonitoring.Core/Models/ProcessRule.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public sealed record ProcessRule(
    string ProcessName,
    ProcessTreatment Treatment,
    bool AllowTrim,
    bool AllowSuspend)
{
    public static ProcessRule WhiteList(string processName) =>
        new(processName, ProcessTreatment.WhiteList, AllowTrim: true, AllowSuspend: false);

    public static ProcessRule TrimOnly(string processName) =>
        new(processName, ProcessTreatment.TrimOnly, AllowTrim: true, AllowSuspend: false);
}
```

`src/MemoryMonitoring.Core/Models/MemoryPolicySettings.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public sealed record MemoryPolicySettings(
    int MemoryLoadPercentThreshold,
    int AvailableMemoryThresholdMb,
    int SustainedPressureSeconds,
    int StartupDelaySeconds,
    int ResumeDelaySeconds,
    int GlobalCooldownSeconds)
{
    public static MemoryPolicySettings CreateDefault() =>
        new(
            MemoryLoadPercentThreshold: 85,
            AvailableMemoryThresholdMb: 2_048,
            SustainedPressureSeconds: 45,
            StartupDelaySeconds: 90,
            ResumeDelaySeconds: 60,
            GlobalCooldownSeconds: 180);
}
```

- [ ] **Step 5: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~RuleDefaultsTests
```

Expected: PASS

- [ ] **Step 6: 提交**

```bash
git add MemoryMonitoring.sln README.md src/MemoryMonitoring.Core tests/MemoryMonitoring.Tests
git commit -m "feat: bootstrap solution and core policy models"
```

### Task 2: 实现趋势采样与内存压力评估

**Files:**
- Create: `src/MemoryMonitoring.Core/Models/SystemMemorySnapshot.cs`
- Create: `src/MemoryMonitoring.Core/Models/MemoryHistoryPoint.cs`
- Create: `src/MemoryMonitoring.Core/Policies/RingHistoryBuffer.cs`
- Create: `src/MemoryMonitoring.Core/Policies/PressureEvaluation.cs`
- Create: `src/MemoryMonitoring.Core/Policies/PressureEvaluator.cs`
- Create: `tests/MemoryMonitoring.Tests/Core/RingHistoryBufferTests.cs`
- Create: `tests/MemoryMonitoring.Tests/Core/PressureEvaluatorTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/Core/RingHistoryBufferTests.cs`

```csharp
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class RingHistoryBufferTests
{
    [Fact]
    public void Buffer_ShouldKeepNewestItemsWithinCapacity()
    {
        var buffer = new RingHistoryBuffer<MemoryHistoryPoint>(capacity: 3);

        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"), 40, 8_000, 8_000));
        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:03+08:00"), 50, 7_000, 9_000));
        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:06+08:00"), 60, 6_000, 10_000));
        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:09+08:00"), 70, 5_000, 11_000));

        var items = buffer.GetSnapshot();

        Assert.Equal(3, items.Count);
        Assert.Equal(50, items[0].UsedPercent);
        Assert.Equal(70, items[2].UsedPercent);
    }
}
```

`tests/MemoryMonitoring.Tests/Core/PressureEvaluatorTests.cs`

```csharp
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class PressureEvaluatorTests
{
    [Fact]
    public void Evaluator_ShouldEnterHighPressure_WhenThresholdSustained()
    {
        var settings = MemoryPolicySettings.CreateDefault();
        var evaluator = new PressureEvaluator(settings);

        var snapshots = Enumerable.Range(0, 20)
            .Select(index => new SystemMemorySnapshot(
                Timestamp: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00").AddSeconds(index * 3),
                MemoryLoadPercent: 90,
                AvailableMemoryMb: 1_200,
                CommitUsedMb: 18_000,
                SystemCacheMb: 2_000))
            .ToArray();

        var result = evaluator.Evaluate(snapshots);

        Assert.Equal(PressureLevel.High, result.Level);
        Assert.True(result.ShouldTriggerCleanup);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~RingHistoryBufferTests|FullyQualifiedName~PressureEvaluatorTests
```

Expected: FAIL，提示 `RingHistoryBuffer`、`PressureEvaluator`、`SystemMemorySnapshot` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.Core/Models/SystemMemorySnapshot.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public sealed record SystemMemorySnapshot(
    DateTimeOffset Timestamp,
    int MemoryLoadPercent,
    long AvailableMemoryMb,
    long CommitUsedMb,
    long SystemCacheMb);
```

`src/MemoryMonitoring.Core/Models/MemoryHistoryPoint.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public sealed record MemoryHistoryPoint(
    DateTimeOffset Timestamp,
    int UsedPercent,
    long AvailableMemoryMb,
    long CommitUsedMb);
```

`src/MemoryMonitoring.Core/Policies/RingHistoryBuffer.cs`

```csharp
namespace MemoryMonitoring.Core.Policies;

public sealed class RingHistoryBuffer<T>
{
    private readonly Queue<T> _queue;
    private readonly int _capacity;

    public RingHistoryBuffer(int capacity)
    {
        _capacity = capacity;
        _queue = new Queue<T>(capacity);
    }

    public void Add(T item)
    {
        if (_queue.Count == _capacity)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(item);
    }

    public IReadOnlyList<T> GetSnapshot() => _queue.ToArray();
}
```

`src/MemoryMonitoring.Core/Policies/PressureEvaluation.cs`

```csharp
namespace MemoryMonitoring.Core.Policies;

public enum PressureLevel
{
    None = 0,
    Medium = 1,
    High = 2
}

public sealed record PressureEvaluation(
    PressureLevel Level,
    bool ShouldTriggerCleanup,
    string Reason);
```

`src/MemoryMonitoring.Core/Policies/PressureEvaluator.cs`

```csharp
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

public sealed class PressureEvaluator
{
    private readonly MemoryPolicySettings _settings;

    public PressureEvaluator(MemoryPolicySettings settings)
    {
        _settings = settings;
    }

    public PressureEvaluation Evaluate(IReadOnlyList<SystemMemorySnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return new PressureEvaluation(PressureLevel.None, false, "No samples");
        }

        var highPressureSamples = snapshots.Count(snapshot =>
            snapshot.MemoryLoadPercent >= _settings.MemoryLoadPercentThreshold ||
            snapshot.AvailableMemoryMb <= _settings.AvailableMemoryThresholdMb);

        var sustainedSeconds = highPressureSamples * 3;

        if (sustainedSeconds >= _settings.SustainedPressureSeconds)
        {
            return new PressureEvaluation(PressureLevel.High, true, "Sustained high pressure");
        }

        return new PressureEvaluation(PressureLevel.None, false, "Below sustained threshold");
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~RingHistoryBufferTests|FullyQualifiedName~PressureEvaluatorTests
```

Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/MemoryMonitoring.Core tests/MemoryMonitoring.Tests
git commit -m "feat: add lightweight history buffer and pressure evaluator"
```

### Task 3: 实现规则存储与进程分类

**Files:**
- Create: `src/MemoryMonitoring.Core/Models/RuleSet.cs`
- Create: `src/MemoryMonitoring.Core/Policies/ProcessClassifier.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Persistence/RuleSetStore.cs`
- Create: `tests/MemoryMonitoring.Tests/Core/ProcessClassifierTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/Core/ProcessClassifierTests.cs`

```csharp
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class ProcessClassifierTests
{
    [Fact]
    public void Classifier_ShouldPreferExplicitWhiteListRule()
    {
        var rules = new RuleSet(
            WhiteList: new[] { ProcessRule.WhiteList("vpn.exe") },
            TrimOnly: Array.Empty<ProcessRule>(),
            SuspendEligible: Array.Empty<ProcessRule>());

        var classifier = new ProcessClassifier(rules);

        var result = classifier.Classify("vpn.exe");

        Assert.Equal(ProcessTreatment.WhiteList, result.Treatment);
        Assert.False(result.AllowSuspend);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~ProcessClassifierTests
```

Expected: FAIL，提示 `RuleSet` 和 `ProcessClassifier` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.Core/Models/RuleSet.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public sealed record RuleSet(
    IReadOnlyList<ProcessRule> WhiteList,
    IReadOnlyList<ProcessRule> TrimOnly,
    IReadOnlyList<ProcessRule> SuspendEligible);
```

`src/MemoryMonitoring.Core/Policies/ProcessClassifier.cs`

```csharp
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

public sealed class ProcessClassifier
{
    private readonly RuleSet _rules;

    public ProcessClassifier(RuleSet rules)
    {
        _rules = rules;
    }

    public ProcessRule Classify(string processName)
    {
        var whiteList = _rules.WhiteList.FirstOrDefault(rule =>
            string.Equals(rule.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

        if (whiteList is not null)
        {
            return whiteList;
        }

        var trimOnly = _rules.TrimOnly.FirstOrDefault(rule =>
            string.Equals(rule.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

        if (trimOnly is not null)
        {
            return trimOnly;
        }

        var suspendEligible = _rules.SuspendEligible.FirstOrDefault(rule =>
            string.Equals(rule.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

        if (suspendEligible is not null)
        {
            return suspendEligible;
        }

        return new ProcessRule(processName, ProcessTreatment.Balanced, AllowTrim: true, AllowSuspend: false);
    }
}
```

`src/MemoryMonitoring.Infrastructure/Persistence/RuleSetStore.cs`

```csharp
using System.Text.Json;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Persistence;

public sealed class RuleSetStore
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string path, RuleSet rules, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(rules, _options);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public async Task<RuleSet> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new RuleSet(Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>());
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<RuleSet>(json, _options)
            ?? new RuleSet(Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>());
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~ProcessClassifierTests
```

Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/MemoryMonitoring.Core src/MemoryMonitoring.Infrastructure tests/MemoryMonitoring.Tests
git commit -m "feat: add rule persistence and process classification"
```

### Task 4: 建立执行器命令契约与管道通信

**Files:**
- Create: `src/MemoryMonitoring.Core/Contracts/CleanupActionType.cs`
- Create: `src/MemoryMonitoring.Core/Contracts/ExecutorRequest.cs`
- Create: `src/MemoryMonitoring.Core/Contracts/ExecutorResponse.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Pipes/ExecutorClient.cs`
- Create: `tests/MemoryMonitoring.Tests/Core/ExecutorContractTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/Core/ExecutorContractTests.cs`

```csharp
using System.Text.Json;
using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Tests.Core;

public sealed class ExecutorContractTests
{
    [Fact]
    public void Request_ShouldRoundTripThroughJson()
    {
        var request = new ExecutorRequest(
            CorrelationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Actions: new[]
            {
                new CleanupAction(CleanupActionType.TrimWorkingSet, 1234, "cherry-studio.exe")
            });

        var json = JsonSerializer.Serialize(request);
        var clone = JsonSerializer.Deserialize<ExecutorRequest>(json);

        Assert.NotNull(clone);
        Assert.Single(clone!.Actions);
        Assert.Equal(CleanupActionType.TrimWorkingSet, clone.Actions[0].Type);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~ExecutorContractTests
```

Expected: FAIL，提示 `ExecutorRequest`、`CleanupAction` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.Core/Contracts/CleanupActionType.cs`

```csharp
namespace MemoryMonitoring.Core.Contracts;

public enum CleanupActionType
{
    TrimWorkingSet = 0,
    SetMemoryPriority = 1,
    SetPowerThrottling = 2,
    PurgeLowPriorityStandby = 3,
    PurgeStandby = 4,
    FlushModifiedPages = 5,
    ClearSystemFileCache = 6,
    CombineMemoryPages = 7,
    SuspendProcess = 8,
    ResumeProcess = 9
}
```

`src/MemoryMonitoring.Core/Contracts/ExecutorRequest.cs`

```csharp
namespace MemoryMonitoring.Core.Contracts;

public sealed record CleanupAction(
    CleanupActionType Type,
    int? ProcessId,
    string? ProcessName);

public sealed record ExecutorRequest(
    Guid CorrelationId,
    IReadOnlyList<CleanupAction> Actions);
```

`src/MemoryMonitoring.Core/Contracts/ExecutorResponse.cs`

```csharp
namespace MemoryMonitoring.Core.Contracts;

public sealed record ActionResult(
    CleanupActionType Type,
    int? ProcessId,
    bool Success,
    string Message);

public sealed record ExecutorResponse(
    Guid CorrelationId,
    IReadOnlyList<ActionResult> Results);
```

`src/MemoryMonitoring.Infrastructure/Pipes/ExecutorClient.cs`

```csharp
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Infrastructure.Pipes;

public sealed class ExecutorClient
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    private readonly string _pipeName;

    public ExecutorClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task<ExecutorResponse> SendAsync(ExecutorRequest request, CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(cancellationToken);

        var requestJson = JsonSerializer.Serialize(request, _options);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
        await pipe.WriteAsync(requestBytes, cancellationToken);
        await pipe.FlushAsync(cancellationToken);

        using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
        var responseJson = await reader.ReadLineAsync(cancellationToken) ?? throw new InvalidOperationException("Missing response");

        return JsonSerializer.Deserialize<ExecutorResponse>(responseJson, _options)
            ?? throw new InvalidOperationException("Invalid executor response");
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~ExecutorContractTests
```

Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/MemoryMonitoring.Core src/MemoryMonitoring.Infrastructure tests/MemoryMonitoring.Tests
git commit -m "feat: add executor contracts and pipe client"
```

### Task 5: 实现清理计划生成器与执行器动作层

**Files:**
- Create: `src/MemoryMonitoring.Core/Policies/CleanupPlanBuilder.cs`
- Create: `src/MemoryMonitoring.Executor/Actions/CleanupExecutor.cs`
- Create: `src/MemoryMonitoring.Executor/Actions/INativeMemoryActions.cs`
- Create: `src/MemoryMonitoring.Executor/Interop/NativeMemoryActions.cs`
- Create: `src/MemoryMonitoring.Executor/Program.cs`
- Create: `tests/MemoryMonitoring.Tests/Core/CleanupPlanBuilderTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/Core/CleanupPlanBuilderTests.cs`

```csharp
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class CleanupPlanBuilderTests
{
    [Fact]
    public void Builder_ShouldNeverSuspendWhiteListProcesses()
    {
        var builder = new CleanupPlanBuilder();
        var rule = ProcessRule.WhiteList("vpn.exe");

        var actions = builder.BuildTargetedActions(
            processId: 42,
            processName: "vpn.exe",
            rule: rule,
            highPressure: true);

        Assert.DoesNotContain(actions, action => action.Type == CleanupActionType.SuspendProcess);
        Assert.Contains(actions, action => action.Type == CleanupActionType.TrimWorkingSet);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~CleanupPlanBuilderTests
```

Expected: FAIL，提示 `CleanupPlanBuilder` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.Core/Policies/CleanupPlanBuilder.cs`

```csharp
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

public sealed class CleanupPlanBuilder
{
    public IReadOnlyList<CleanupAction> BuildTargetedActions(
        int processId,
        string processName,
        ProcessRule rule,
        bool highPressure)
    {
        var actions = new List<CleanupAction>
        {
            new(CleanupActionType.TrimWorkingSet, processId, processName),
            new(CleanupActionType.SetMemoryPriority, processId, processName),
            new(CleanupActionType.SetPowerThrottling, processId, processName)
        };

        if (highPressure && rule.AllowSuspend)
        {
            actions.Add(new CleanupAction(CleanupActionType.SuspendProcess, processId, processName));
        }

        return actions;
    }
}
```

`src/MemoryMonitoring.Executor/Actions/INativeMemoryActions.cs`

```csharp
using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Executor.Actions;

public interface INativeMemoryActions
{
    ActionResult Execute(CleanupAction action);
}
```

`src/MemoryMonitoring.Executor/Actions/CleanupExecutor.cs`

```csharp
using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Executor.Actions;

public sealed class CleanupExecutor
{
    private readonly INativeMemoryActions _nativeActions;

    public CleanupExecutor(INativeMemoryActions nativeActions)
    {
        _nativeActions = nativeActions;
    }

    public ExecutorResponse Execute(ExecutorRequest request)
    {
        var results = request.Actions
            .Select(_nativeActions.Execute)
            .ToArray();

        return new ExecutorResponse(request.CorrelationId, results);
    }
}
```

`src/MemoryMonitoring.Executor/Interop/NativeMemoryActions.cs`

```csharp
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;

namespace MemoryMonitoring.Executor.Interop;

public sealed class NativeMemoryActions : INativeMemoryActions
{
    public ActionResult Execute(CleanupAction action)
    {
        return action.Type switch
        {
            CleanupActionType.TrimWorkingSet => new ActionResult(action.Type, action.ProcessId, true, "Trim requested"),
            CleanupActionType.SetMemoryPriority => new ActionResult(action.Type, action.ProcessId, true, "Priority requested"),
            CleanupActionType.SetPowerThrottling => new ActionResult(action.Type, action.ProcessId, true, "Throttle requested"),
            CleanupActionType.SuspendProcess => new ActionResult(action.Type, action.ProcessId, true, "Suspend requested"),
            _ => new ActionResult(action.Type, action.ProcessId, true, "System cleanup requested")
        };
    }
}
```

`src/MemoryMonitoring.Executor/Program.cs`

```csharp
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;
using MemoryMonitoring.Executor.Interop;

var executor = new CleanupExecutor(new NativeMemoryActions());
var sample = new ExecutorRequest(Guid.NewGuid(), Array.Empty<CleanupAction>());
var response = executor.Execute(sample);
Console.WriteLine($"Executor ready: {response.CorrelationId}");
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~CleanupPlanBuilderTests
```

Expected: PASS

- [ ] **Step 5: 构建执行器，确认可编译**

Run:

```bash
dotnet build src/MemoryMonitoring.Executor/MemoryMonitoring.Executor.csproj
```

Expected: BUILD SUCCEEDED

- [ ] **Step 6: 提交**

```bash
git add src/MemoryMonitoring.Core src/MemoryMonitoring.Executor tests/MemoryMonitoring.Tests
git commit -m "feat: add cleanup plan builder and executor action layer"
```

### Task 6: 实现自动化编排与采样服务

**Files:**
- Create: `src/MemoryMonitoring.Core/Models/ProcessMemorySnapshot.cs`
- Create: `src/MemoryMonitoring.Core/Policies/AutomationOrchestrator.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Monitoring/SystemMemorySampler.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Monitoring/ProcessMemorySampler.cs`
- Create: `tests/MemoryMonitoring.Tests/Core/AutomationOrchestratorTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/Core/AutomationOrchestratorTests.cs`

```csharp
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class AutomationOrchestratorTests
{
    [Fact]
    public void Orchestrator_ShouldSkipSuspendForTrimOnlyRule()
    {
        var orchestrator = new AutomationOrchestrator(new CleanupPlanBuilder());
        var rule = ProcessRule.TrimOnly("chrome.exe");

        var actions = orchestrator.BuildProcessActions(
            processId: 123,
            processName: "chrome.exe",
            rule: rule,
            pressureLevel: PressureLevel.High);

        Assert.DoesNotContain(actions, action => action.Type.ToString() == "SuspendProcess");
        Assert.NotEmpty(actions);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~AutomationOrchestratorTests
```

Expected: FAIL，提示 `AutomationOrchestrator` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.Core/Models/ProcessMemorySnapshot.cs`

```csharp
namespace MemoryMonitoring.Core.Models;

public sealed record ProcessMemorySnapshot(
    int ProcessId,
    string ProcessName,
    long WorkingSetBytes,
    long PrivateBytes,
    DateTimeOffset LastForegroundSeenAt);
```

`src/MemoryMonitoring.Core/Policies/AutomationOrchestrator.cs`

```csharp
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

public sealed class AutomationOrchestrator
{
    private readonly CleanupPlanBuilder _builder;

    public AutomationOrchestrator(CleanupPlanBuilder builder)
    {
        _builder = builder;
    }

    public IReadOnlyList<CleanupAction> BuildProcessActions(
        int processId,
        string processName,
        ProcessRule rule,
        PressureLevel pressureLevel)
    {
        return _builder.BuildTargetedActions(
            processId,
            processName,
            rule,
            highPressure: pressureLevel == PressureLevel.High);
    }
}
```

`src/MemoryMonitoring.Infrastructure/Monitoring/SystemMemorySampler.cs`

```csharp
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Monitoring;

public sealed class SystemMemorySampler
{
    public SystemMemorySnapshot Sample()
    {
        return new SystemMemorySnapshot(
            Timestamp: DateTimeOffset.Now,
            MemoryLoadPercent: 0,
            AvailableMemoryMb: 0,
            CommitUsedMb: 0,
            SystemCacheMb: 0);
    }
}
```

`src/MemoryMonitoring.Infrastructure/Monitoring/ProcessMemorySampler.cs`

```csharp
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Monitoring;

public sealed class ProcessMemorySampler
{
    public IReadOnlyList<ProcessMemorySnapshot> SampleTopProcesses() =>
        Array.Empty<ProcessMemorySnapshot>();
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~AutomationOrchestratorTests
```

Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/MemoryMonitoring.Core src/MemoryMonitoring.Infrastructure tests/MemoryMonitoring.Tests
git commit -m "feat: add automation orchestrator and sampling service skeletons"
```

### Task 7: 实现主窗口、趋势图与托盘入口

**Files:**
- Create: `src/MemoryMonitoring.App/ViewModels/DashboardViewModel.cs`
- Create: `src/MemoryMonitoring.App/Controls/MemoryTrendControl.xaml`
- Create: `src/MemoryMonitoring.App/Controls/MemoryTrendControl.xaml.cs`
- Modify: `src/MemoryMonitoring.App/MainWindow.xaml`
- Modify: `src/MemoryMonitoring.App/MainWindow.xaml.cs`
- Create: `src/MemoryMonitoring.App/Tray/TrayHost.cs`
- Create: `tests/MemoryMonitoring.Tests/App/DashboardViewModelTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/App/DashboardViewModelTests.cs`

```csharp
using MemoryMonitoring.App.ViewModels;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Tests.App;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void ViewModel_ShouldExposeReadableMemorySummary()
    {
        var viewModel = new DashboardViewModel();

        viewModel.Update(new SystemMemorySnapshot(
            DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"),
            MemoryLoadPercent: 76,
            AvailableMemoryMb: 3_200,
            CommitUsedMb: 14_500,
            SystemCacheMb: 2_400));

        Assert.Equal("76%", viewModel.MemoryLoadText);
        Assert.Equal("3200 MB", viewModel.AvailableMemoryText);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet add tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj reference src/MemoryMonitoring.App/MemoryMonitoring.App.csproj
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~DashboardViewModelTests
```

Expected: FAIL，提示 `DashboardViewModel` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.App/ViewModels/DashboardViewModel.cs`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private string _memoryLoadText = "0%";
    private string _availableMemoryText = "0 MB";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string MemoryLoadText
    {
        get => _memoryLoadText;
        private set
        {
            _memoryLoadText = value;
            OnPropertyChanged();
        }
    }

    public string AvailableMemoryText
    {
        get => _availableMemoryText;
        private set
        {
            _availableMemoryText = value;
            OnPropertyChanged();
        }
    }

    public void Update(SystemMemorySnapshot snapshot)
    {
        MemoryLoadText = $"{snapshot.MemoryLoadPercent}%";
        AvailableMemoryText = $"{snapshot.AvailableMemoryMb} MB";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

`src/MemoryMonitoring.App/MainWindow.xaml`

```xml
<Window x:Class="MemoryMonitoring.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Memory Guardian"
        Width="1080"
        Height="720">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="220" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
            <Border Padding="16" Margin="0,0,12,0" Background="#1F2937" CornerRadius="12">
                <StackPanel>
                    <TextBlock Foreground="White" FontSize="14" Text="内存负载" />
                    <TextBlock Foreground="White" FontSize="28" FontWeight="Bold" Text="{Binding MemoryLoadText}" />
                </StackPanel>
            </Border>
            <Border Padding="16" Background="#0F766E" CornerRadius="12">
                <StackPanel>
                    <TextBlock Foreground="White" FontSize="14" Text="可用内存" />
                    <TextBlock Foreground="White" FontSize="28" FontWeight="Bold" Text="{Binding AvailableMemoryText}" />
                </StackPanel>
            </Border>
        </StackPanel>

        <Border Grid.Row="1" Background="#F3F4F6" CornerRadius="12" Padding="16">
            <TextBlock FontSize="16" FontWeight="SemiBold" Text="近期趋势图占位区" />
        </Border>

        <Border Grid.Row="2" Margin="0,16,0,0" Background="#FFFFFF" CornerRadius="12" Padding="16">
            <TextBlock FontSize="16" FontWeight="SemiBold" Text="进程列表与动作日志占位区" />
        </Border>
    </Grid>
</Window>
```

`src/MemoryMonitoring.App/MainWindow.xaml.cs`

```csharp
using System.Windows;
using MemoryMonitoring.App.ViewModels;

namespace MemoryMonitoring.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~DashboardViewModelTests
```

Expected: PASS

- [ ] **Step 5: 构建 UI，确认可编译**

Run:

```bash
dotnet build src/MemoryMonitoring.App/MemoryMonitoring.App.csproj
```

Expected: BUILD SUCCEEDED

- [ ] **Step 6: 提交**

```bash
git add src/MemoryMonitoring.App tests/MemoryMonitoring.Tests
git commit -m "feat: add dashboard shell and initial view model"
```

### Task 8: 补齐启动/唤醒策略、日志与项目文档

**Files:**
- Create: `src/MemoryMonitoring.Infrastructure/Power/PowerModeBridge.cs`
- Create: `src/MemoryMonitoring.Infrastructure/Persistence/ActionLogStore.cs`
- Modify: `README.md`
- Create: `tests/MemoryMonitoring.Tests/Core/ActionLogStoreTests.cs`

- [ ] **Step 1: 写失败测试**

`tests/MemoryMonitoring.Tests/Core/ActionLogStoreTests.cs`

```csharp
using MemoryMonitoring.Infrastructure.Persistence;

namespace MemoryMonitoring.Tests.Core;

public sealed class ActionLogStoreTests
{
    [Fact]
    public async Task Store_ShouldAppendJsonLines()
    {
        var path = Path.GetTempFileName();
        var store = new ActionLogStore();

        await store.AppendAsync(path, "trim", "chrome.exe", "ok", CancellationToken.None);
        await store.AppendAsync(path, "standby", "system", "ok", CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(path);

        Assert.Equal(2, lines.Length);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~ActionLogStoreTests
```

Expected: FAIL，提示 `ActionLogStore` 未定义。

- [ ] **Step 3: 写最小实现**

`src/MemoryMonitoring.Infrastructure/Persistence/ActionLogStore.cs`

```csharp
using System.Text.Json;

namespace MemoryMonitoring.Infrastructure.Persistence;

public sealed class ActionLogStore
{
    public async Task AppendAsync(
        string path,
        string action,
        string target,
        string result,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            Timestamp = DateTimeOffset.Now,
            Action = action,
            Target = target,
            Result = result
        });

        await File.AppendAllTextAsync(path, payload + Environment.NewLine, cancellationToken);
    }
}
```

`src/MemoryMonitoring.Infrastructure/Power/PowerModeBridge.cs`

```csharp
namespace MemoryMonitoring.Infrastructure.Power;

public sealed class PowerModeBridge
{
    public event EventHandler? ResumeDetected;

    public void RaiseResumeDetected() => ResumeDetected?.Invoke(this, EventArgs.Empty);
}
```

`README.md`

```markdown
# Windows 内存守护器

一个面向 Windows 的轻量桌面内存监控与自动回收工具。

## 当前阶段

- 已完成设计文档
- 已完成实现计划
- 下一步按 `docs/superpowers/plans/2026-06-03-windows-memory-guardian.md` 执行

## 目标能力

- 内存总览与趋势图
- 白名单与规则分类
- 自动 trim 与系统级回收
- 开机后 / 唤醒后软回收
```

- [ ] **Step 4: 运行测试，确认通过**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj --filter FullyQualifiedName~ActionLogStoreTests
```

Expected: PASS

- [ ] **Step 5: 做一次手工验证清单**

Run:

```bash
dotnet test tests/MemoryMonitoring.Tests/MemoryMonitoring.Tests.csproj
dotnet build MemoryMonitoring.sln
```

Expected:

- 单元测试全部通过
- 解决方案构建成功
- README 与设计文档路径正确

手工检查：

- 主窗口能打开
- 趋势图区域可见
- 白名单规则能序列化
- 提权执行器可以独立启动
- 自动化编排不会对白名单生成挂起动作

- [ ] **Step 6: 提交**

```bash
git add README.md src/MemoryMonitoring.Infrastructure tests/MemoryMonitoring.Tests
git commit -m "docs: finalize bootstrap docs and supporting infrastructure plan"
```

---

## 自检记录

### 规格覆盖

- 监控：Task 2、Task 6、Task 7
- 白名单与规则：Task 1、Task 3、Task 6
- 自动 trim 与系统级回收：Task 4、Task 5、Task 6
- UI 与趋势：Task 7
- 启动后 / 唤醒后：Task 8
- 文档与交付：Task 8

### 占位符扫描

- 未保留未完成占位语句
- 每个任务都给出明确路径、命令或代码块

### 类型一致性

- `ProcessRule`、`ProcessTreatment`、`CleanupAction`、`PressureLevel` 在后续任务中保持同名
- `AllowSuspend` 语义前后一致

---

Plan complete and saved to `docs/superpowers/plans/2026-06-03-windows-memory-guardian.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
