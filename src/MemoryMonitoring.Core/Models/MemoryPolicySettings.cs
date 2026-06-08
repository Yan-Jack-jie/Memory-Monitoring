namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 定义自动内存治理的全局阈值设置。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record MemoryPolicySettings(
    int MemoryLoadPercentThreshold,
    int AvailableMemoryThresholdMb,
    int SustainedPressureSeconds,
    int StartupDelaySeconds,
    int ResumeDelaySeconds,
    int GlobalCooldownSeconds,
    bool StartWithWindows = false)
{
    public static MemoryPolicySettings CreateDefault() =>
        new(
            MemoryLoadPercentThreshold: 85,
            AvailableMemoryThresholdMb: 2_048,
            SustainedPressureSeconds: 45,
            StartupDelaySeconds: 90,
            ResumeDelaySeconds: 60,
            GlobalCooldownSeconds: 180,
            StartWithWindows: false);
}
