using MemoryMonitoring.Infrastructure.Persistence;

namespace MemoryMonitoring.Tests.Core;

public sealed class ActionLogStoreTests
{
    [Fact]
    public async Task Store_ShouldAppendJsonLines()
    {
        var path = Path.GetTempFileName();
        var store = new ActionLogStore();

        await store.AppendAsync(path, "trim", "chrome.exe", "ok", CancellationToken.None);
        await store.AppendAsync(path, "standby", "system", "ok", CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(path);

        Assert.Equal(2, lines.Length);
    }
}
