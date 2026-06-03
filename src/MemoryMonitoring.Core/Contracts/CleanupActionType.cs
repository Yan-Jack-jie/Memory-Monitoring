namespace MemoryMonitoring.Core.Contracts;

/// <summary>
/// 定义执行器支持的清理动作类型。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public enum CleanupActionType
{
    TrimWorkingSet = 0,
    SetMemoryPriority = 1,
    SetPowerThrottling = 2,
    PurgeLowPriorityStandby = 3,
    PurgeStandby = 4,
    FlushModifiedPages = 5,
    ClearSystemFileCache = 6,
    CombineMemoryPages = 7,
    SuspendProcess = 8,
    ResumeProcess = 9
}
