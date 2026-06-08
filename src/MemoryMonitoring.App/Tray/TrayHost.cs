using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;
using FormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using FormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using FormsToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;

namespace MemoryMonitoring.App.Tray;

/// <summary>
/// 管理系统托盘入口与窗口恢复请求。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class TrayHost : IDisposable
{
    private readonly ITrayIcon _trayIcon;

    public event EventHandler? RestoreRequested;

    public event EventHandler? ExitRequested;

    public bool IsInitialized { get; private set; }

    public TrayHost()
        : this(new WinFormsTrayIcon())
    {
    }

    public TrayHost(ITrayIcon trayIcon)
    {
        _trayIcon = trayIcon;
        _trayIcon.Activated += TrayIcon_OnActivated;
        _trayIcon.ExitRequested += TrayIcon_OnExitRequested;
    }

    public void Initialize()
    {
        _trayIcon.Text = "Memory Guardian";
        _trayIcon.Visible = true;
        IsInitialized = true;
    }

    public void Dispose()
    {
        _trayIcon.Activated -= TrayIcon_OnActivated;
        _trayIcon.ExitRequested -= TrayIcon_OnExitRequested;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private void TrayIcon_OnActivated(object? sender, EventArgs e) =>
        RestoreRequested?.Invoke(this, EventArgs.Empty);

    private void TrayIcon_OnExitRequested(object? sender, EventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);
}

public interface ITrayIcon : IDisposable
{
    event EventHandler? Activated;

    event EventHandler? ExitRequested;

    string Text { get; set; }

    bool Visible { get; set; }
}

internal sealed class WinFormsTrayIcon : ITrayIcon
{
    private readonly FormsNotifyIcon _notifyIcon;
    private readonly FormsContextMenuStrip _menu;

    public event EventHandler? Activated;

    public event EventHandler? ExitRequested;

    public WinFormsTrayIcon()
    {
        _menu = new FormsContextMenuStrip();
        var restoreItem = new FormsToolStripMenuItem("显示主窗口");
        restoreItem.Click += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        var exitItem = new FormsToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _menu.Items.Add(restoreItem);
        _menu.Items.Add(exitItem);

        _notifyIcon = new FormsNotifyIcon
        {
            Icon = DrawingSystemIcons.Application,
            ContextMenuStrip = _menu
        };
        _notifyIcon.DoubleClick += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
    }

    public string Text
    {
        get => _notifyIcon.Text;
        set => _notifyIcon.Text = value;
    }

    public bool Visible
    {
        get => _notifyIcon.Visible;
        set => _notifyIcon.Visible = value;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Icon = (DrawingIcon?)null;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
