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
