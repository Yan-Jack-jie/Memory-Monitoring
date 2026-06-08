using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 根据清理前后的系统可用内存估算回收量。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public static class MemoryReclaimEstimator
{
    public static string FormatReclaimedMemory(
        SystemMemorySnapshot before,
        SystemMemorySnapshot after,
        bool actionSucceeded)
    {
        if (!actionSucceeded)
        {
            return "-";
        }

        var reclaimedMb = Math.Max(after.AvailableMemoryMb - before.AvailableMemoryMb, 0);
        return $"{reclaimedMb} MB";
    }
}
