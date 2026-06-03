namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 表示某一时刻的系统内存快照。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record SystemMemorySnapshot(
    DateTimeOffset Timestamp,
    int MemoryLoadPercent,
    long AvailableMemoryMb,
    long CommitUsedMb,
    long SystemCacheMb);
