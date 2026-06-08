using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 根据规则和压力级别生成进程级清理动作。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
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

    public IReadOnlyList<CleanupAction> BuildSystemActions(bool highPressure)
    {
        var actions = new List<CleanupAction>
        {
            new(CleanupActionType.PurgeLowPriorityStandby, null, "System")
        };

        if (highPressure)
        {
            actions.Add(new CleanupAction(CleanupActionType.PurgeStandby, null, "System"));
            actions.Add(new CleanupAction(CleanupActionType.FlushModifiedPages, null, "System"));
        }

        return actions;
    }
}
