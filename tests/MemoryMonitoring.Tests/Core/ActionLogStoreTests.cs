using MemoryMonitoring.Infrastructure.Persistence;

namespace MemoryMonitoring.Tests.Core;

public sealed class ActionLogStoreTests
{
    [Fact]
    public async Task Store_ShouldAppendJsonLines()
    {
        var path = Path.GetTempFileName();
        var store = new ActionLogStore();

        await store.AppendAsync(path, "trim", "chrome.exe", "ok", "128 MB", CancellationToken.None);
        await store.AppendAsync(path, "standby", "system", "ok", "256 MB", CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(path);

        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public async Task LoadRecentAsync_ShouldSkipBrokenLinesUntilEnoughEntriesAreLoaded()
    {
        var path = Path.GetTempFileName();
        var lines = new[]
        {
            """{"Timestamp":"2026-06-03T10:00:00+08:00","Action":"old","Target":"a.exe","Result":"ok","ReclaimedMemory":"1 MB"}""",
            """{"Timestamp":"2026-06-03T10:01:00+08:00","Action":"middle","Target":"b.exe","Result":"ok","ReclaimedMemory":"2 MB"}""",
            "not-json",
            """{"Timestamp":"2026-06-03T10:02:00+08:00","Action":"new","Target":"c.exe","Result":"ok","ReclaimedMemory":"3 MB"}"""
        };
        await File.WriteAllLinesAsync(path, lines);
        var store = new ActionLogStore();

        var entries = await store.LoadRecentAsync(path, 2, CancellationToken.None);

        Assert.Equal(2, entries.Count);
        Assert.Equal("new", entries[0].Action);
        Assert.Equal("middle", entries[1].Action);
    }
}
