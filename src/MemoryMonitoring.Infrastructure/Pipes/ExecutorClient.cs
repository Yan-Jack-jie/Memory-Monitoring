using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Infrastructure.Pipes;

/// <summary>
/// 负责与提权执行器进行命名管道通信。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class ExecutorClient
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    private readonly string _pipeName;

    public ExecutorClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task<ExecutorResponse> SendAsync(ExecutorRequest request, CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(cancellationToken);

        var requestJson = JsonSerializer.Serialize(request, _options);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
        await pipe.WriteAsync(requestBytes, cancellationToken);
        await pipe.FlushAsync(cancellationToken);

        using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
        var responseJson = await reader.ReadLineAsync(cancellationToken) ?? throw new InvalidOperationException("Missing response");

        return JsonSerializer.Deserialize<ExecutorResponse>(responseJson, _options)
            ?? throw new InvalidOperationException("Invalid executor response");
    }
}
