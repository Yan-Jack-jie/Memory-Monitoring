namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 表示单个进程的内存占用快照。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ProcessMemorySnapshot(
    int ProcessId,
    string ProcessName,
    long WorkingSetBytes,
    long PrivateBytes,
    DateTimeOffset LastForegroundSeenAt);
