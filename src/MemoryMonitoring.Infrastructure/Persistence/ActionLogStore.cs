using MemoryMonitoring.Core.Models;
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
        string reclaimedMemory,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            Timestamp = DateTimeOffset.Now,
            Action = action,
            Target = target,
            Result = result,
            ReclaimedMemory = reclaimedMemory
        });

        await File.AppendAllTextAsync(path, payload + Environment.NewLine, cancellationToken);
    }

    public async Task<IReadOnlyList<ActionLogEntry>> LoadRecentAsync(string path, int take, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return Array.Empty<ActionLogEntry>();
        }

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var entries = new List<ActionLogEntry>();

        foreach (var line in lines.Reverse().Take(take))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                entries.Add(new ActionLogEntry(
                    root.GetProperty("Timestamp").GetDateTimeOffset(),
                    root.GetProperty("Action").GetString() ?? "-",
                    root.GetProperty("Target").GetString() ?? "-",
                    root.GetProperty("Result").GetString() ?? "-",
                    root.TryGetProperty("ReclaimedMemory", out var reclaimedElement)
                        ? reclaimedElement.GetString() ?? "-"
                        : "-"));
            }
            catch
            {
                // 忽略损坏的日志行
            }
        }

        return entries;
    }
}
