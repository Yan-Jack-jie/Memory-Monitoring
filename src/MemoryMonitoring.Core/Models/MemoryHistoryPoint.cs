namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 用于趋势图展示的轻量历史点。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record MemoryHistoryPoint(
    DateTimeOffset Timestamp,
    int UsedPercent,
    long AvailableMemoryMb,
    long CommitUsedMb);
