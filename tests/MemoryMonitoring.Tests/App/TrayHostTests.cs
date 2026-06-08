using MemoryMonitoring.App.Tray;

namespace MemoryMonitoring.Tests.App;

public sealed class TrayHostTests
{
    [Fact]
    public void Initialize_ShouldShowTrayIconAndExposeRestoreRequest()
    {
        var icon = new FakeTrayIcon();
        var requested = false;
        using var trayHost = new TrayHost(icon);
        trayHost.RestoreRequested += (_, _) => requested = true;

        trayHost.Initialize();
        icon.RaiseActivated();

        Assert.True(trayHost.IsInitialized);
        Assert.True(icon.Visible);
        Assert.Equal("Memory Guardian", icon.Text);
        Assert.True(requested);
    }

    [Fact]
    public void Dispose_ShouldHideTrayIcon()
    {
        var icon = new FakeTrayIcon();
        var trayHost = new TrayHost(icon);

        trayHost.Initialize();
        trayHost.Dispose();

        Assert.False(icon.Visible);
        Assert.True(icon.IsDisposed);
    }

    private sealed class FakeTrayIcon : ITrayIcon
    {
        public event EventHandler? Activated;

        public event EventHandler? ExitRequested
        {
            add { }
            remove { }
        }

        public bool IsDisposed { get; private set; }

        public string Text { get; set; } = string.Empty;

        public bool Visible { get; set; }

        public void RaiseActivated() => Activated?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
