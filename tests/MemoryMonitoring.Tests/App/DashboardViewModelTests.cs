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
        Assert.Equal("3200 MB", viewModel.AvailableMemoryText);
    }
}
