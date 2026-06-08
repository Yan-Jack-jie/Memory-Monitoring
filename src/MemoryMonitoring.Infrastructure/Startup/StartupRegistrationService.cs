using System.Runtime.Versioning;

namespace MemoryMonitoring.Infrastructure.Startup;

/// <summary>
/// 管理 Memory Guardian 的当前用户开机自启注册。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class StartupRegistrationService
{
    public const string ApplicationName = "Memory Guardian";

    private readonly IStartupRegistry _registry;

    [SupportedOSPlatform("windows")]
    public StartupRegistrationService()
        : this(new CurrentUserStartupRegistry())
    {
    }

    public StartupRegistrationService(IStartupRegistry registry)
    {
        _registry = registry;
    }

    public void Register(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("启动路径不能为空。", nameof(executablePath));
        }

        _registry.SetValue(ApplicationName, QuotePath(executablePath));
    }

    public void Unregister()
    {
        _registry.DeleteValue(ApplicationName);
    }

    private static string QuotePath(string executablePath)
    {
        var trimmed = executablePath.Trim().Trim('"');
        return $"\"{trimmed}\"";
    }
}
