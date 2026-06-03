namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 表示单条动作日志记录。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ActionLogEntry(
    DateTimeOffset Timestamp,
    string Action,
    string Target,
    string Result,
    string ReclaimedMemory);
