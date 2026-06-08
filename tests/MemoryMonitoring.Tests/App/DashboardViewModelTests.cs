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
}
