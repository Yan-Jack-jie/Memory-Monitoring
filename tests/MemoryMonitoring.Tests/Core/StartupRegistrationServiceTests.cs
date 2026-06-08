using MemoryMonitoring.Infrastructure.Startup;

namespace MemoryMonitoring.Tests.Core;

public sealed class StartupRegistrationServiceTests
{
    [Fact]
    public void Register_ShouldWriteQuotedExecutablePathToRunKey()
    {
        var registry = new FakeStartupRegistry();
        var service = new StartupRegistrationService(registry);

        service.Register("D:\\Apps\\Memory Guardian\\MemoryMonitoring.App.exe");

        Assert.Equal(
            "\"D:\\Apps\\Memory Guardian\\MemoryMonitoring.App.exe\"",
            registry.Values[StartupRegistrationService.ApplicationName]);
    }

    [Fact]
    public void Unregister_ShouldRemoveRunKeyValue()
    {
        var registry = new FakeStartupRegistry();
        var service = new StartupRegistrationService(registry);
        service.Register("D:\\Apps\\Memory Guardian\\MemoryMonitoring.App.exe");

        service.Unregister();

        Assert.DoesNotContain(StartupRegistrationService.ApplicationName, registry.Values.Keys);
    }

    private sealed class FakeStartupRegistry : IStartupRegistry
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void SetValue(string name, string value)
        {
            Values[name] = value;
        }

        public void DeleteValue(string name)
        {
            Values.Remove(name);
        }
    }
}
