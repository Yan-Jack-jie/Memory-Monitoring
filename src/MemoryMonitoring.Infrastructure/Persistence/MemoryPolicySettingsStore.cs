using System.Text.Json;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Persistence;

/// <summary>
/// 负责自动化策略设置的本地读写。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class MemoryPolicySettingsStore
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string path, MemoryPolicySettings settings, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(settings, _options);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public async Task<MemoryPolicySettings> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return MemoryPolicySettings.CreateDefault();
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<MemoryPolicySettings>(json, _options)
            ?? MemoryPolicySettings.CreateDefault();
    }
}
