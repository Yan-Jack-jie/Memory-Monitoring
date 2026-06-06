using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Infrastructure.Execution;

/// <summary>
/// 在独立执行器不可用时，提供明确失败的兜底网关。
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
                Success: false,
                Message: "执行器不可用，未执行清理动作"))
            .ToArray();

        return Task.FromResult(new ExecutorResponse(request.CorrelationId, results));
    }
}
