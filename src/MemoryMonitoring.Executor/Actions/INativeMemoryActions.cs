using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Executor.Actions;

/// <summary>
/// 定义执行器的底层动作接口。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public interface INativeMemoryActions
{
    ActionResult Execute(CleanupAction action);
}
