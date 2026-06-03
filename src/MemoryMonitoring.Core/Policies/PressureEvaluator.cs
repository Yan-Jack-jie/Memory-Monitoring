using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 根据近期采样判断系统是否处于持续内存压力中。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class PressureEvaluator
{
    private readonly MemoryPolicySettings _settings;

    public PressureEvaluator(MemoryPolicySettings settings)
    {
        _settings = settings;
    }

    public PressureEvaluation Evaluate(IReadOnlyList<SystemMemorySnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return new PressureEvaluation(PressureLevel.None, false, "No samples");
        }

        var highPressureSamples = snapshots.Count(snapshot =>
            snapshot.MemoryLoadPercent >= _settings.MemoryLoadPercentThreshold ||
            snapshot.AvailableMemoryMb <= _settings.AvailableMemoryThresholdMb);

        var sustainedSeconds = highPressureSamples * 3;

        if (sustainedSeconds >= _settings.SustainedPressureSeconds)
        {
            return new PressureEvaluation(PressureLevel.High, true, "Sustained high pressure");
        }

        return new PressureEvaluation(PressureLevel.None, false, "Below sustained threshold");
    }
}
