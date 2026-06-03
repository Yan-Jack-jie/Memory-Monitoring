using System.Diagnostics;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Monitoring;

/// <summary>
/// 提供进程级内存采样。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class ProcessMemorySampler
{
    public IReadOnlyList<ProcessMemorySnapshot> SampleTopProcesses(int take = 8)
    {
        var snapshots = new List<ProcessMemorySnapshot>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                snapshots.Add(new ProcessMemorySnapshot(
                    ProcessId: process.Id,
                    ProcessName: string.IsNullOrWhiteSpace(process.ProcessName) ? "Unknown" : $"{process.ProcessName}.exe",
                    WorkingSetBytes: process.WorkingSet64,
                    PrivateBytes: process.PrivateMemorySize64,
                    LastForegroundSeenAt: DateTimeOffset.Now));
            }
            catch
            {
                // 部分系统进程无权限读取，忽略即可。
            }
            finally
            {
                process.Dispose();
            }
        }

        return snapshots
            .OrderByDescending(item => item.WorkingSetBytes)
            .Take(take)
            .ToArray();
    }
}
