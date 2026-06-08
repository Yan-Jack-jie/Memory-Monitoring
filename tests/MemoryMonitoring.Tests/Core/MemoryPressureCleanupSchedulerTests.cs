using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class MemoryPressureCleanupSchedulerTests
{
    [Fact]
    public void AddSample_ShouldReturnHighPressureSystemActions_WhenPressureIsSustained()
    {
        var scheduler = new MemoryPressureCleanupScheduler(
            MemoryPolicySettings.CreateDefault(),
            new CleanupPlanBuilder());

        IReadOnlyList<CleanupAction> actions = Array.Empty<CleanupAction>();
        for (var index = 0; index < 20; index++)
        {
            var currentActions = scheduler.AddSample(new SystemMemorySnapshot(
                Timestamp: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00").AddSeconds(index * 3),
                MemoryLoadPercent: 90,
                AvailableMemoryMb: 1_000,
                CommitUsedMb: 20_000,
                SystemCacheMb: 2_000));

            if (currentActions.Count > 0)
            {
                actions = currentActions;
            }
        }

        Assert.Contains(actions, action => action.Type == CleanupActionType.PurgeStandby);
        Assert.Contains(actions, action => action.Type == CleanupActionType.FlushModifiedPages);
    }

    [Fact]
    public void AddSample_ShouldRespectGlobalCooldown()
    {
        var scheduler = new MemoryPressureCleanupScheduler(
            MemoryPolicySettings.CreateDefault(),
            new CleanupPlanBuilder());

        IReadOnlyList<CleanupAction> firstActions = Array.Empty<CleanupAction>();
        for (var index = 0; index < 20; index++)
        {
            var currentActions = scheduler.AddSample(CreateHighPressureSample(index));
            if (currentActions.Count > 0)
            {
                firstActions = currentActions;
            }
        }

        var secondActions = scheduler.AddSample(CreateHighPressureSample(21));

        Assert.NotEmpty(firstActions);
        Assert.Empty(secondActions);
    }

    private static SystemMemorySnapshot CreateHighPressureSample(int index) =>
        new(
            Timestamp: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00").AddSeconds(index * 3),
            MemoryLoadPercent: 90,
            AvailableMemoryMb: 1_000,
            CommitUsedMb: 20_000,
            SystemCacheMb: 2_000);
}
