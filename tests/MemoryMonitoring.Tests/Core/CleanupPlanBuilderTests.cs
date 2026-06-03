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
