using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class PressureEvaluatorTests
{
    [Fact]
    public void Evaluator_ShouldEnterHighPressure_WhenThresholdSustained()
    {
        var settings = MemoryPolicySettings.CreateDefault();
        var evaluator = new PressureEvaluator(settings);

        var snapshots = Enumerable.Range(0, 20)
            .Select(index => new SystemMemorySnapshot(
                Timestamp: DateTimeOffset.Parse("2026-06-03T10:00:00+08:00").AddSeconds(index * 3),
                MemoryLoadPercent: 90,
                AvailableMemoryMb: 1_200,
                CommitUsedMb: 18_000,
                SystemCacheMb: 2_000))
            .ToArray();

        var result = evaluator.Evaluate(snapshots);

        Assert.Equal(PressureLevel.High, result.Level);
        Assert.True(result.ShouldTriggerCleanup);
    }
}
