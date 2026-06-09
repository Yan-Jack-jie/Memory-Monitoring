using MemoryMonitoring.App.ViewModels;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Tests.App;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void ViewModel_ShouldExposeReadableMemorySummary()
    {
        var viewModel = new DashboardViewModel();

        viewModel.Update(new SystemMemorySnapshot(
            DateTimeOffset.Parse("2026-06-03T10:00:00+08:00"),
            MemoryLoadPercent: 76,
            AvailableMemoryMb: 3_200,
            CommitUsedMb: 14_500,
            SystemCacheMb: 2_400));

        Assert.Equal("76%", viewModel.MemoryLoadText);
        Assert.Equal("3.1 GB", viewModel.AvailableMemoryText);
    }

    [Fact]
    public void Update_ShouldAppendMemoryHistoryPointsWithinCapacity()
    {
        var viewModel = new DashboardViewModel();

        for (var index = 0; index < 140; index++)
        {
            viewModel.Update(new SystemMemorySnapshot(
                DateTimeOffset.Parse("2026-06-03T10:00:00+08:00").AddSeconds(index * 3),
                MemoryLoadPercent: index % 100,
                AvailableMemoryMb: 8_000 - index,
                CommitUsedMb: 10_000 + index,
                SystemCacheMb: 2_000));
        }

        Assert.Equal(120, viewModel.MemoryHistory.Count);
        Assert.Equal(20, viewModel.MemoryHistory[0].UsedPercent);
        Assert.Equal(39, viewModel.MemoryHistory[^1].UsedPercent);
    }

    [Fact]
    public void LoadDefaultAutomationSettings_ShouldExposeStartWithWindowsState()
    {
        var viewModel = new DashboardViewModel();
        var settings = new MemoryPolicySettings(80, 1536, 30, 60, 45, 120, StartWithWindows: true);

        viewModel.LoadDefaultAutomationSettings(settings);

        Assert.True(viewModel.StartWithWindows);
    }

    [Fact]
    public void LoadDefaultAutomationSettings_ShouldExposeProtectionSettings()
    {
        var viewModel = new DashboardViewModel();
        var settings = new MemoryPolicySettings(
            80,
            1536,
            30,
            60,
            45,
            120,
            ProtectForegroundProcesses: false,
            ProtectNetworkSensitiveProcesses: false,
            NewProcessProtectionSeconds: 0);

        viewModel.LoadDefaultAutomationSettings(settings);

        Assert.False(viewModel.ProtectForegroundProcesses);
        Assert.False(viewModel.ProtectNetworkSensitiveProcesses);
        Assert.False(viewModel.ProtectNewProcesses);
    }

    [Fact]
    public void MarkExecutorUnavailable_ShouldExposeRetryState()
    {
        var viewModel = new DashboardViewModel();

        viewModel.MarkExecutorUnavailable("执行器不可用，未执行清理动作");

        Assert.Equal("执行器不可用，可重试", viewModel.AutomationStatusText);
        Assert.Equal("执行器不可用，未执行清理动作", viewModel.ExecutorStatusDetailText);
        Assert.True(viewModel.CanRetryExecutor);
    }

    [Fact]
    public void MarkExecutorHealthy_ShouldClearRetryState()
    {
        var viewModel = new DashboardViewModel();
        viewModel.MarkExecutorUnavailable("执行器不可用，未执行清理动作");

        viewModel.MarkExecutorHealthy();

        Assert.Equal("运行中", viewModel.AutomationStatusText);
        Assert.Equal("执行器连接正常", viewModel.ExecutorStatusDetailText);
        Assert.False(viewModel.CanRetryExecutor);
    }
}
