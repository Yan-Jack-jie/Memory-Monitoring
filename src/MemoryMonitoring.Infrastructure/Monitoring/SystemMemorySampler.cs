using System.Runtime.InteropServices;
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
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            throw new InvalidOperationException("无法读取系统内存状态。");
        }

        var totalPhysicalMb = ToMb(status.TotalPhys);
        var availablePhysicalMb = ToMb(status.AvailPhys);
        var totalCommitMb = ToMb(status.TotalPageFile);
        var availableCommitMb = ToMb(status.AvailPageFile);
        var commitUsedMb = Math.Max(totalCommitMb - availableCommitMb, 0);
        var systemCacheMb = Math.Max(totalPhysicalMb - availablePhysicalMb - ToMb(status.TotalVirtual - status.AvailVirtual), 0);

        return new SystemMemorySnapshot(
            Timestamp: DateTimeOffset.Now,
            MemoryLoadPercent: (int)status.MemoryLoad,
            AvailableMemoryMb: availablePhysicalMb,
            CommitUsedMb: commitUsedMb,
            SystemCacheMb: systemCacheMb);
    }

    private static long ToMb(ulong bytes) => (long)(bytes / 1024 / 1024);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}
