namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 聚合所有规则分类集合。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record RuleSet(
    IReadOnlyList<ProcessRule> WhiteList,
    IReadOnlyList<ProcessRule> TrimOnly,
    IReadOnlyList<ProcessRule> SuspendEligible);
