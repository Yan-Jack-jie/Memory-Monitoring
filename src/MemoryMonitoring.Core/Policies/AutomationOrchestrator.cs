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

    public IReadOnlyList<CleanupAction> BuildProcessPlan(
        IReadOnlyList<ProcessMemorySnapshot> processes,
        ProcessClassifier classifier,
        MemoryPolicySettings settings,
        PressureLevel pressureLevel,
        DateTimeOffset evaluatedAt,
        Func<ProcessMemorySnapshot, ProcessProtectionContext>? protectionFactory = null)
    {
        var actions = new List<CleanupAction>();
        foreach (var process in processes)
        {
            var rule = classifier.Classify(process.ProcessName);
            if (rule.Treatment == ProcessTreatment.WhiteList)
            {
                continue;
            }

            var protection = protectionFactory?.Invoke(process)
                ?? BuildDefaultProtectionContext(process, evaluatedAt);
            actions.AddRange(_builder.BuildTargetedActions(
                process.ProcessId,
                process.ProcessName,
                rule,
                highPressure: pressureLevel == PressureLevel.High,
                settings,
                protection));
        }

        return actions;
    }

    private static ProcessProtectionContext BuildDefaultProtectionContext(
        ProcessMemorySnapshot process,
        DateTimeOffset evaluatedAt)
    {
        return new ProcessProtectionContext(
            IsForeground: process.LastForegroundSeenAt >= evaluatedAt.AddSeconds(-30),
            IsNetworkSensitive: IsNetworkSensitiveProcess(process.ProcessName),
            StartedAt: null,
            EvaluatedAt: evaluatedAt);
    }

    private static bool IsNetworkSensitiveProcess(string processName)
    {
        return processName.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
            processName.Contains("wireguard", StringComparison.OrdinalIgnoreCase) ||
            processName.Contains("tailscale", StringComparison.OrdinalIgnoreCase) ||
            processName.Contains("zerotier", StringComparison.OrdinalIgnoreCase);
    }
}
