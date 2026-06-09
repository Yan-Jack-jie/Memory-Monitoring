using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;
using MemoryMonitoring.Core.Contracts;

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

    [Fact]
    public void BuildProcessPlan_ShouldSkipWhiteListAndPlanEligibleProcesses()
    {
        var rules = new RuleSet(
            WhiteList: new[] { ProcessRule.WhiteList("vpn.exe") },
            TrimOnly: new[] { ProcessRule.TrimOnly("browser.exe") },
            SuspendEligible: new[] { new ProcessRule("sync.exe", ProcessTreatment.SuspendEligible, true, true) });
        var orchestrator = new AutomationOrchestrator(new CleanupPlanBuilder());

        var actions = orchestrator.BuildProcessPlan(
            new[]
            {
                CreateProcess(10, "vpn.exe"),
                CreateProcess(11, "browser.exe"),
                CreateProcess(12, "sync.exe")
            },
            new ProcessClassifier(rules),
            MemoryPolicySettings.CreateDefault(),
            PressureLevel.High,
            DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"));

        Assert.DoesNotContain(actions, action => action.ProcessId == 10);
        Assert.Contains(actions, action => action.ProcessId == 11 && action.Type == CleanupActionType.TrimWorkingSet);
        Assert.Contains(actions, action => action.ProcessId == 12 && action.Type == CleanupActionType.SuspendProcess);
    }

    [Fact]
    public void BuildProcessPlan_ShouldRespectProtectionContext()
    {
        var rules = new RuleSet(
            WhiteList: Array.Empty<ProcessRule>(),
            TrimOnly: Array.Empty<ProcessRule>(),
            SuspendEligible: new[] { new ProcessRule("editor.exe", ProcessTreatment.SuspendEligible, true, true) });
        var orchestrator = new AutomationOrchestrator(new CleanupPlanBuilder());

        var actions = orchestrator.BuildProcessPlan(
            new[] { CreateProcess(42, "editor.exe") },
            new ProcessClassifier(rules),
            MemoryPolicySettings.CreateDefault(),
            PressureLevel.High,
            DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"),
            process => new ProcessProtectionContext(
                IsForeground: process.ProcessName == "editor.exe",
                IsNetworkSensitive: false,
                StartedAt: DateTimeOffset.Parse("2026-06-03T09:50:00+08:00"),
                EvaluatedAt: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00")));

        Assert.Empty(actions);
    }

    private static ProcessMemorySnapshot CreateProcess(int processId, string processName) =>
        new(
            ProcessId: processId,
            ProcessName: processName,
            WorkingSetBytes: 512L * 1024 * 1024,
            PrivateBytes: 256L * 1024 * 1024,
            LastForegroundSeenAt: DateTimeOffset.Parse("2026-06-03T09:50:00+08:00"));
}
