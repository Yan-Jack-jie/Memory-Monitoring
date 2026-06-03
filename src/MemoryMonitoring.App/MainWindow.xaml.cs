using System.Windows;
using MemoryMonitoring.App.ViewModels;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.App;

/// <summary>
/// 主窗口逻辑。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new DashboardViewModel();
        viewModel.Update(new SystemMemorySnapshot(
            DateTimeOffset.Now,
            MemoryLoadPercent: 68,
            AvailableMemoryMb: 5_100,
            CommitUsedMb: 12_600,
            SystemCacheMb: 3_200));

        DataContext = viewModel;
    }
}
