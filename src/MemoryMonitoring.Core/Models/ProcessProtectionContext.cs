namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 描述进程在生成自动清理计划时需要考虑的保护状态。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ProcessProtectionContext(
    bool IsForeground,
    bool IsNetworkSensitive,
    DateTimeOffset? StartedAt,
    DateTimeOffset EvaluatedAt);
