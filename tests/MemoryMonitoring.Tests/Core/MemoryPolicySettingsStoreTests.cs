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
        var settings = new MemoryPolicySettings(80, 1536, 30, 60, 45, 120, StartWithWindows: true);

        await store.SaveAsync(path, settings, CancellationToken.None);
        var loaded = await store.LoadAsync(path, CancellationToken.None);

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public async Task LoadAsync_ShouldReturnDefaultsWhenFileIsCorrupted()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "{not-valid-json");
        var store = new MemoryPolicySettingsStore();

        var loaded = await store.LoadAsync(path, CancellationToken.None);

        Assert.Equal(MemoryPolicySettings.CreateDefault(), loaded);
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateParentDirectoryWhenMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        var store = new MemoryPolicySettingsStore();

        await store.SaveAsync(path, MemoryPolicySettings.CreateDefault(), CancellationToken.None);

        Assert.True(File.Exists(path));
    }
}
