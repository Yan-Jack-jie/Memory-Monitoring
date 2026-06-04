using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;

namespace MemoryMonitoring.Executor.Services;

/// <summary>
/// 提供基于命名管道的执行器服务端循环。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class ExecutorServer
{
    private readonly CleanupExecutor _executor;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public ExecutorServer(CleanupExecutor executor)
    {
        _executor = executor;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                ExecutorConstants.PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(cancellationToken);

            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true)
            {
                AutoFlush = true
            };

            var requestJson = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(requestJson))
            {
                continue;
            }

            var request = JsonSerializer.Deserialize<ExecutorRequest>(requestJson, _options);
            if (request is null)
            {
                continue;
            }

            var response = _executor.Execute(request);
            var responseJson = JsonSerializer.Serialize(response, _options);
            await writer.WriteLineAsync(responseJson);
        }
    }
}
