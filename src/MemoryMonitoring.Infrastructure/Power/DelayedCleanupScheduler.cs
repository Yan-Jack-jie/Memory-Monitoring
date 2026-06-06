using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Infrastructure.Power;

/// <summary>
/// 根据启动和唤醒延迟策略调度软回收动作。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class DelayedCleanupScheduler
{
    private readonly PowerModeBridge _powerModeBridge;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public DelayedCleanupScheduler(PowerModeBridge powerModeBridge)
        : this(powerModeBridge, Task.Delay)
    {
    }

    public DelayedCleanupScheduler(
        PowerModeBridge powerModeBridge,
        Func<TimeSpan, CancellationToken, Task> delayAsync)
    {
        _powerModeBridge = powerModeBridge;
        _delayAsync = delayAsync;
    }

    public async Task ScheduleStartupAsync(
        MemoryPolicySettings settings,
        Func<CancellationToken, Task> cleanupAsync,
        CancellationToken cancellationToken)
    {
        await DelayThenCleanupAsync(
            TimeSpan.FromSeconds(settings.StartupDelaySeconds),
            cleanupAsync,
            cancellationToken);
    }

    public void ConfigureResumeCleanup(
        MemoryPolicySettings settings,
        Func<CancellationToken, Task> cleanupAsync,
        CancellationToken cancellationToken)
    {
        _powerModeBridge.ResumeDetected += async (_, _) =>
        {
            await DelayThenCleanupAsync(
                TimeSpan.FromSeconds(settings.ResumeDelaySeconds),
                cleanupAsync,
                cancellationToken);
        };
    }

    private async Task DelayThenCleanupAsync(
        TimeSpan delay,
        Func<CancellationToken, Task> cleanupAsync,
        CancellationToken cancellationToken)
    {
        await _delayAsync(delay, cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            await cleanupAsync(cancellationToken);
        }
    }
}
