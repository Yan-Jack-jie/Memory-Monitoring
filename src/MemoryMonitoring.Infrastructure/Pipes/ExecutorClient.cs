using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
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
    private readonly string? _executorPath;

    public ExecutorClient(string pipeName, string? executorPath = null)
    {
        _pipeName = pipeName;
        _executorPath = executorPath;
    }

    public async Task<ExecutorResponse> SendAsync(ExecutorRequest request, CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            await pipe.ConnectAsync(500, cancellationToken);
        }
        catch (TimeoutException)
        {
            TryStartExecutor();
            await pipe.ConnectAsync(3000, cancellationToken);
        }

        var requestJson = JsonSerializer.Serialize(request, _options);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
        await pipe.WriteAsync(requestBytes, cancellationToken);
        await pipe.FlushAsync(cancellationToken);

        using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
        var responseJson = await reader.ReadLineAsync(cancellationToken) ?? throw new InvalidOperationException("Missing response");

        return JsonSerializer.Deserialize<ExecutorResponse>(responseJson, _options)
            ?? throw new InvalidOperationException("Invalid executor response");
    }

    private void TryStartExecutor()
    {
        if (string.IsNullOrWhiteSpace(_executorPath) || !File.Exists(_executorPath))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _executorPath,
            Arguments = "--serve",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);
    }
}
