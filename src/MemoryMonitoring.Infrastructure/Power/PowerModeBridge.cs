namespace MemoryMonitoring.Infrastructure.Power;

/// <summary>
/// 对接系统唤醒等电源事件。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class PowerModeBridge
{
    public event EventHandler? ResumeDetected;

    public void RaiseResumeDetected() => ResumeDetected?.Invoke(this, EventArgs.Empty);
}
