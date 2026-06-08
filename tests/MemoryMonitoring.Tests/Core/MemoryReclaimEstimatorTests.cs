using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class MemoryReclaimEstimatorTests
{
    [Fact]
    public void FormatReclaimedMemory_ShouldReturnAvailableMemoryIncrease()
    {
        var before = CreateSnapshot(availableMemoryMb: 1_024);
        var after = CreateSnapshot(availableMemoryMb: 1_280);

        var reclaimed = MemoryReclaimEstimator.FormatReclaimedMemory(before, after, actionSucceeded: true);

        Assert.Equal("256 MB", reclaimed);
    }

    [Fact]
    public void FormatReclaimedMemory_ShouldReturnZeroWhenAvailableMemoryDrops()
    {
        var before = CreateSnapshot(availableMemoryMb: 1_280);
        var after = CreateSnapshot(availableMemoryMb: 1_024);

        var reclaimed = MemoryReclaimEstimator.FormatReclaimedMemory(before, after, actionSucceeded: true);

        Assert.Equal("0 MB", reclaimed);
    }

    [Fact]
    public void FormatReclaimedMemory_ShouldReturnDashWhenActionFails()
    {
        var before = CreateSnapshot(availableMemoryMb: 1_024);
        var after = CreateSnapshot(availableMemoryMb: 1_280);

        var reclaimed = MemoryReclaimEstimator.FormatReclaimedMemory(before, after, actionSucceeded: false);

        Assert.Equal("-", reclaimed);
    }

    private static SystemMemorySnapshot CreateSnapshot(long availableMemoryMb) =>
        new(
            Timestamp: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"),
            MemoryLoadPercent: 80,
            AvailableMemoryMb: availableMemoryMb,
            CommitUsedMb: 10_000,
            SystemCacheMb: 2_000);
}
