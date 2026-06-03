using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Executor.Actions;

/// <summary>
/// 顺序执行批量清理命令。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class CleanupExecutor
{
    private readonly INativeMemoryActions _nativeActions;

    public CleanupExecutor(INativeMemoryActions nativeActions)
    {
        _nativeActions = nativeActions;
    }

    public ExecutorResponse Execute(ExecutorRequest request)
    {
        var results = request.Actions
            .Select(_nativeActions.Execute)
            .ToArray();

        return new ExecutorResponse(request.CorrelationId, results);
    }
}
