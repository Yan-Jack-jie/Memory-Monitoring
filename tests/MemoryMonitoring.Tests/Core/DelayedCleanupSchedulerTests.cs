using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Infrastructure.Power;

namespace MemoryMonitoring.Tests.Core;

public sealed class DelayedCleanupSchedulerTests
{
    [Fact]
    public async Task ScheduleStartupAsync_ShouldUseStartupDelayBeforeCleanup()
    {
        var delays = new List<TimeSpan>();
        var cleanupCount = 0;
        using var bridge = new PowerModeBridge(new ManualPowerModeEventSource());
        var scheduler = new DelayedCleanupScheduler(bridge, (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });

        await scheduler.ScheduleStartupAsync(
            MemoryPolicySettings.CreateDefault(),
            _ =>
            {
                cleanupCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromSeconds(90), delays[0]);
        Assert.Equal(1, cleanupCount);
    }

    [Fact]
    public void ResumeDetected_ShouldUseResumeDelayBeforeCleanup()
    {
        var delays = new List<TimeSpan>();
        var cleanupCount = 0;
        var source = new ManualPowerModeEventSource();
        using var bridge = new PowerModeBridge(source);
        var scheduler = new DelayedCleanupScheduler(bridge, (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });

        scheduler.ConfigureResumeCleanup(
            MemoryPolicySettings.CreateDefault(),
            _ =>
            {
                cleanupCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        source.RaiseResumed();

        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromSeconds(60), delays[0]);
        Assert.Equal(1, cleanupCount);
    }
}
