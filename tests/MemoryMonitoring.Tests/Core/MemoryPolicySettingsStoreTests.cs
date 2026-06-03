using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Infrastructure.Persistence;

namespace MemoryMonitoring.Tests.Core;

public sealed class MemoryPolicySettingsStoreTests
{
    [Fact]
    public async Task Store_ShouldRoundTripSettings()
    {
        var path = Path.GetTempFileName();
        var store = new MemoryPolicySettingsStore();
        var settings = new MemoryPolicySettings(80, 1536, 30, 60, 45, 120);

        await store.SaveAsync(path, settings, CancellationToken.None);
        var loaded = await store.LoadAsync(path, CancellationToken.None);

        Assert.Equal(settings, loaded);
    }
}
