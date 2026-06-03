using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 协调自动化策略与动作生成。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
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
