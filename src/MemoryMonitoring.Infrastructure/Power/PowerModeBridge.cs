using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MemoryMonitoring.Infrastructure.Power;

/// <summary>
/// 定义电源模式事件来源，便于测试和替换系统事件实现。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public interface IPowerModeEventSource
{
    event EventHandler? Resumed;
}

/// <summary>
/// 对接系统唤醒等电源事件。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class PowerModeBridge : IDisposable
{
    private readonly IPowerModeEventSource _eventSource;
    private bool _disposed;

    [SupportedOSPlatform("windows")]
    public PowerModeBridge()
        : this(new WindowsPowerModeEventSource())
    {
    }

    public PowerModeBridge(IPowerModeEventSource eventSource)
    {
        _eventSource = eventSource;
        _eventSource.Resumed += EventSource_OnResumed;
    }

    public event EventHandler? ResumeDetected;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _eventSource.Resumed -= EventSource_OnResumed;
        if (_eventSource is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }

    public void RaiseResumeDetected() => ResumeDetected?.Invoke(this, EventArgs.Empty);

    private void EventSource_OnResumed(object? sender, EventArgs e) => RaiseResumeDetected();
}

/// <summary>
/// 默认手动事件源，供未接入系统事件时保持兼容。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class ManualPowerModeEventSource : IPowerModeEventSource
{
    public event EventHandler? Resumed;

    public void RaiseResumed() => Resumed?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 将 Windows 电源模式变化事件转换为应用内部唤醒事件。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPowerModeEventSource : IPowerModeEventSource, IDisposable
{
    private bool _disposed;

    public WindowsPowerModeEventSource()
    {
        SystemEvents.PowerModeChanged += SystemEvents_OnPowerModeChanged;
    }

    public event EventHandler? Resumed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        SystemEvents.PowerModeChanged -= SystemEvents_OnPowerModeChanged;
        _disposed = true;
    }

    private void SystemEvents_OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Resumed?.Invoke(this, EventArgs.Empty);
        }
    }
}
