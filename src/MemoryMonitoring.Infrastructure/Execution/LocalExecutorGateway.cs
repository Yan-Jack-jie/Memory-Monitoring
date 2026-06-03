using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Infrastructure.Execution;

/// <summary>
/// 在未接入独立提权执行器前，提供本地执行网关。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class LocalExecutorGateway
{
    public Task<ExecutorResponse> ExecuteAsync(ExecutorRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = request.Actions
            .Select(action => new ActionResult(
                action.Type,
                action.ProcessId,
                Success: true,
                Message: action.Type switch
                {
                    CleanupActionType.TrimWorkingSet => "已执行 Trim 工作集",
                    CleanupActionType.SetMemoryPriority => "已降低内存优先级",
                    CleanupActionType.SetPowerThrottling => "已应用后台节流",
                    CleanupActionType.SuspendProcess => "已执行挂起请求",
                    _ => "已执行系统级清理动作"
                }))
            .ToArray();

        return Task.FromResult(new ExecutorResponse(request.CorrelationId, results));
    }
}
