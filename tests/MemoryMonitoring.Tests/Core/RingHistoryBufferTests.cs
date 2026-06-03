using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class RingHistoryBufferTests
{
    [Fact]
    public void Buffer_ShouldKeepNewestItemsWithinCapacity()
    {
        var buffer = new RingHistoryBuffer<MemoryHistoryPoint>(capacity: 3);

        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"), 40, 8_000, 8_000));
        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:03+08:00"), 50, 7_000, 9_000));
        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:06+08:00"), 60, 6_000, 10_000));
        buffer.Add(new MemoryHistoryPoint(DateTimeOffset.Parse("2026-06-03T10:00:09+08:00"), 70, 5_000, 11_000));

        var items = buffer.GetSnapshot();

        Assert.Equal(3, items.Count);
        Assert.Equal(50, items[0].UsedPercent);
        Assert.Equal(70, items[2].UsedPercent);
    }
}
