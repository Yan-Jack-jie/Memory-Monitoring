using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;

namespace MemoryMonitoring.Executor.Interop;

/// <summary>
/// 第一版底层动作桩实现，后续替换为真实 Win32/PInvoke 调用。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class NativeMemoryActions : INativeMemoryActions
{
    public ActionResult Execute(CleanupAction action)
    {
        return action.Type switch
        {
            CleanupActionType.TrimWorkingSet => new ActionResult(action.Type, action.ProcessId, true, "Trim requested"),
            CleanupActionType.SetMemoryPriority => new ActionResult(action.Type, action.ProcessId, true, "Priority requested"),
            CleanupActionType.SetPowerThrottling => new ActionResult(action.Type, action.ProcessId, true, "Throttle requested"),
            CleanupActionType.SuspendProcess => new ActionResult(action.Type, action.ProcessId, true, "Suspend requested"),
            _ => new ActionResult(action.Type, action.ProcessId, true, "System cleanup requested")
        };
    }
}
