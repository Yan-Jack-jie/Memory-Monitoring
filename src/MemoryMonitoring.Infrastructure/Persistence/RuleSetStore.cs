using System.Text.Json;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Persistence;

/// <summary>
/// 负责规则集合的本地读写。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class RuleSetStore
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string path, RuleSet rules, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(rules, _options);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public async Task<RuleSet> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new RuleSet(Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>());
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<RuleSet>(json, _options)
            ?? new RuleSet(Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>(), Array.Empty<ProcessRule>());
    }
}
