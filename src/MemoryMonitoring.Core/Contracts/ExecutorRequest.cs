namespace MemoryMonitoring.Core.Contracts;

/// <summary>
/// 描述执行器的一次动作请求。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record CleanupAction(
    CleanupActionType Type,
    int? ProcessId,
    string? ProcessName);

/// <summary>
/// 描述发送给执行器的批量请求。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ExecutorRequest(
    Guid CorrelationId,
    IReadOnlyList<CleanupAction> Actions);
