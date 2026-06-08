using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MemoryMonitoring.Infrastructure.Startup;

/// <summary>
/// 通过 HKCU Run 注册表项维护当前用户开机自启。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CurrentUserStartupRegistry : IStartupRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void SetValue(string name, string value)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(name, value, RegistryValueKind.String);
    }

    public void DeleteValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
