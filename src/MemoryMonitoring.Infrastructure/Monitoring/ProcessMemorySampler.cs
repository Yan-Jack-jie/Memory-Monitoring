using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Monitoring;

/// <summary>
/// 提供进程级内存采样。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class ProcessMemorySampler
{
    public IReadOnlyList<ProcessMemorySnapshot> SampleTopProcesses() =>
        Array.Empty<ProcessMemorySnapshot>();
}
