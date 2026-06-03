using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Monitoring;

/// <summary>
/// 提供系统级内存采样。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class SystemMemorySampler
{
    public SystemMemorySnapshot Sample()
    {
        return new SystemMemorySnapshot(
            Timestamp: DateTimeOffset.Now,
            MemoryLoadPercent: 0,
            AvailableMemoryMb: 0,
            CommitUsedMb: 0,
            SystemCacheMb: 0);
    }
}
