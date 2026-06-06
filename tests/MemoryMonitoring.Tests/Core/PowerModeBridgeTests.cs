using MemoryMonitoring.Infrastructure.Power;

namespace MemoryMonitoring.Tests.Core;

public sealed class PowerModeBridgeTests
{
    [Fact]
    public void PowerResume_ShouldRaiseResumeDetected()
    {
        var source = new TestPowerModeEventSource();
        using var bridge = new PowerModeBridge(source);
        var resumeCount = 0;
        bridge.ResumeDetected += (_, _) => resumeCount++;

        source.RaiseResumed();

        Assert.Equal(1, resumeCount);
    }

    [Fact]
    public void Dispose_ShouldStopListeningToPowerEvents()
    {
        var source = new TestPowerModeEventSource();
        var bridge = new PowerModeBridge(source);
        var resumeCount = 0;
        bridge.ResumeDetected += (_, _) => resumeCount++;

        bridge.Dispose();
        source.RaiseResumed();

        Assert.Equal(0, resumeCount);
    }

    private sealed class TestPowerModeEventSource : IPowerModeEventSource
    {
        public event EventHandler? Resumed;

        public void RaiseResumed() => Resumed?.Invoke(this, EventArgs.Empty);
    }
}
