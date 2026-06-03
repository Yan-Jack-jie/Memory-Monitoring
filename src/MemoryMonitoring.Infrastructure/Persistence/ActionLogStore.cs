using System.Text.Json;

namespace MemoryMonitoring.Infrastructure.Persistence;

/// <summary>
/// 负责将动作日志追加到 JSONL 文件。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class ActionLogStore
{
    public async Task AppendAsync(
        string path,
        string action,
        string target,
        string result,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            Timestamp = DateTimeOffset.Now,
            Action = action,
            Target = target,
            Result = result
        });

        await File.AppendAllTextAsync(path, payload + Environment.NewLine, cancellationToken);
    }
}
