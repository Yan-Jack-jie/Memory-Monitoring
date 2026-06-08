using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 根据持续内存压力和全局冷却时间生成自动系统清理计划。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class MemoryPressureCleanupScheduler
{
    private const int SampleIntervalSeconds = 3;
    private readonly CleanupPlanBuilder _cleanupPlanBuilder;
    private readonly PressureEvaluator _pressureEvaluator;
    private readonly RingHistoryBuffer<SystemMemorySnapshot> _samples;
    private readonly MemoryPolicySettings _settings;
    private DateTimeOffset? _lastCleanupAt;

    public MemoryPressureCleanupScheduler(
        MemoryPolicySettings settings,
        CleanupPlanBuilder cleanupPlanBuilder)
    {
        _settings = settings;
        _cleanupPlanBuilder = cleanupPlanBuilder;
        _pressureEvaluator = new PressureEvaluator(settings);
        var capacity = Math.Max(settings.SustainedPressureSeconds / SampleIntervalSeconds + 4, 8);
        _samples = new RingHistoryBuffer<SystemMemorySnapshot>(capacity);
    }

    public IReadOnlyList<CleanupAction> AddSample(SystemMemorySnapshot snapshot)
    {
        _samples.Add(snapshot);
        var evaluation = _pressureEvaluator.Evaluate(_samples.GetSnapshot());

        if (!evaluation.ShouldTriggerCleanup || IsCoolingDown(snapshot.Timestamp))
        {
            return Array.Empty<CleanupAction>();
        }

        _lastCleanupAt = snapshot.Timestamp;
        return _cleanupPlanBuilder.BuildSystemActions(highPressure: evaluation.Level == PressureLevel.High);
    }

    private bool IsCoolingDown(DateTimeOffset timestamp)
    {
        if (_lastCleanupAt is null)
        {
            return false;
        }

        return timestamp - _lastCleanupAt < TimeSpan.FromSeconds(_settings.GlobalCooldownSeconds);
    }
}
