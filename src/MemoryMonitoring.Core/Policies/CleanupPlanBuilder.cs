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
        return BuildTargetedActions(
            processId,
            processName,
            rule,
            highPressure,
            MemoryPolicySettings.CreateDefault(),
            protection: null);
    }

    public IReadOnlyList<CleanupAction> BuildTargetedActions(
        int processId,
        string processName,
        ProcessRule rule,
        bool highPressure,
        MemoryPolicySettings settings,
        ProcessProtectionContext? protection)
    {
        if (IsProtected(settings, protection))
        {
            return Array.Empty<CleanupAction>();
        }

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

    private static bool IsProtected(MemoryPolicySettings settings, ProcessProtectionContext? protection)
    {
        if (protection is null)
        {
            return false;
        }

        if (settings.ProtectForegroundProcesses && protection.IsForeground)
        {
            return true;
        }

        if (settings.ProtectNetworkSensitiveProcesses && protection.IsNetworkSensitive)
        {
            return true;
        }

        if (protection.StartedAt is null || settings.NewProcessProtectionSeconds <= 0)
        {
            return false;
        }

        return protection.EvaluatedAt - protection.StartedAt.Value
            < TimeSpan.FromSeconds(settings.NewProcessProtectionSeconds);
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
