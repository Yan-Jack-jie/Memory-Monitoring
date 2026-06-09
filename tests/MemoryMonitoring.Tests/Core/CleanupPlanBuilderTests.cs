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

    [Fact]
    public void BuildSystemActions_ShouldUseOnlyStableSoftCleanupActionsByDefault()
    {
        var builder = new CleanupPlanBuilder();

        var actions = builder.BuildSystemActions(highPressure: false);

        Assert.Single(actions);
        Assert.Contains(actions, action => action.Type == CleanupActionType.PurgeLowPriorityStandby);
        Assert.DoesNotContain(actions, action => action.Type == CleanupActionType.CombineMemoryPages);
        Assert.DoesNotContain(actions, action => action.Type == CleanupActionType.ClearSystemFileCache);
    }

    [Fact]
    public void BuildSystemActions_ShouldAddStrongerStableActionsUnderHighPressure()
    {
        var builder = new CleanupPlanBuilder();

        var actions = builder.BuildSystemActions(highPressure: true);

        Assert.Contains(actions, action => action.Type == CleanupActionType.PurgeLowPriorityStandby);
        Assert.Contains(actions, action => action.Type == CleanupActionType.PurgeStandby);
        Assert.Contains(actions, action => action.Type == CleanupActionType.FlushModifiedPages);
        Assert.DoesNotContain(actions, action => action.Type == CleanupActionType.ClearSystemFileCache);
    }

    [Fact]
    public void BuildTargetedActions_ShouldSkipProtectedForegroundProcess()
    {
        var builder = new CleanupPlanBuilder();
        var settings = MemoryPolicySettings.CreateDefault();

        var actions = builder.BuildTargetedActions(
            processId: 42,
            processName: "editor.exe",
            rule: new ProcessRule("editor.exe", ProcessTreatment.Balanced, true, true),
            highPressure: true,
            settings: settings,
            protection: new ProcessProtectionContext(
                IsForeground: true,
                IsNetworkSensitive: false,
                StartedAt: DateTimeOffset.Parse("2026-06-03T09:50:00+08:00"),
                EvaluatedAt: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00")));

        Assert.Empty(actions);
    }

    [Fact]
    public void BuildTargetedActions_ShouldSkipNetworkSensitiveProcess()
    {
        var builder = new CleanupPlanBuilder();
        var settings = MemoryPolicySettings.CreateDefault();

        var actions = builder.BuildTargetedActions(
            processId: 42,
            processName: "vpn.exe",
            rule: new ProcessRule("vpn.exe", ProcessTreatment.Balanced, true, true),
            highPressure: true,
            settings: settings,
            protection: new ProcessProtectionContext(
                IsForeground: false,
                IsNetworkSensitive: true,
                StartedAt: DateTimeOffset.Parse("2026-06-03T09:50:00+08:00"),
                EvaluatedAt: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00")));

        Assert.Empty(actions);
    }

    [Fact]
    public void BuildTargetedActions_ShouldSkipNewProcessWithinProtectionWindow()
    {
        var builder = new CleanupPlanBuilder();
        var settings = MemoryPolicySettings.CreateDefault();

        var actions = builder.BuildTargetedActions(
            processId: 42,
            processName: "fresh.exe",
            rule: new ProcessRule("fresh.exe", ProcessTreatment.Balanced, true, true),
            highPressure: true,
            settings: settings,
            protection: new ProcessProtectionContext(
                IsForeground: false,
                IsNetworkSensitive: false,
                StartedAt: DateTimeOffset.Parse("2026-06-03T09:59:00+08:00"),
                EvaluatedAt: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00")));

        Assert.Empty(actions);
    }
}
