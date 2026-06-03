namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 定义进程在自动化策略中的处理方式。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public enum ProcessTreatment
{
    WhiteList = 0,
    TrimOnly = 1,
    Balanced = 2,
    SuspendEligible = 3
}
