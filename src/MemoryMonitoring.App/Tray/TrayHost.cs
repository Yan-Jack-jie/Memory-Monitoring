namespace MemoryMonitoring.App.Tray;

/// <summary>
/// 托盘宿主预留类型，后续接入通知图标。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class TrayHost
{
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = true;
    }
}
