namespace MemoryMonitoring.Core.Contracts;

/// <summary>
/// 描述单个动作的执行结果。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ActionResult(
    CleanupActionType Type,
    int? ProcessId,
    bool Success,
    string Message);

/// <summary>
/// 描述执行器返回的批量结果。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ExecutorResponse(
    Guid CorrelationId,
    IReadOnlyList<ActionResult> Results);
