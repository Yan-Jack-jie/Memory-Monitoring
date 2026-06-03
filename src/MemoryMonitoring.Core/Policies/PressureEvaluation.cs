namespace MemoryMonitoring.Core.Policies;

/// <summary>
/// 描述压力评估结果。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public enum PressureLevel
{
    None = 0,
    Medium = 1,
    High = 2
}

/// <summary>
/// 压力评估的返回对象。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record PressureEvaluation(
    PressureLevel Level,
    bool ShouldTriggerCleanup,
    string Reason);
