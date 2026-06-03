using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 根据规则集为进程选择处理方式。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
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
