using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MemoryMonitoring.App.ViewModels;
using MemoryMonitoring.Infrastructure.Monitoring;

namespace MemoryMonitoring.App;

/// <summary>
/// 主窗口逻辑。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public partial class MainWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly ProcessMemorySampler _processMemorySampler;
    private readonly SystemMemorySampler _systemMemorySampler;
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow()
    {
        InitializeComponent();

        _systemMemorySampler = new SystemMemorySampler();
        _processMemorySampler = new ProcessMemorySampler();
        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;

        RefreshDashboard();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _refreshTimer.Tick += (_, _) => RefreshDashboard();
        _refreshTimer.Start();
    }

    private void RefreshDashboard()
    {
        try
        {
            var snapshot = _systemMemorySampler.Sample();
            var processes = _processMemorySampler.SampleTopProcesses();
            _viewModel.Update(snapshot, processes);
        }
        catch
        {
            // 采样失败时先保持上一次界面状态，避免窗口抖动。
        }
    }

    private void DashboardNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(DashboardPage, DashboardNavButton);

    private void ProcessesNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(ProcessesPage, ProcessesNavButton);

    private void RulesNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(RulesPage, RulesNavButton);

    private void AutomationNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(AutomationPage, AutomationNavButton);

    private void HistoryNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(HistoryPage, HistoryNavButton);

    private void SettingsNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(SettingsPage, SettingsNavButton);

    private void ActivatePage(UIElement targetPage, Button activeButton)
    {
        DashboardPage.Visibility = Visibility.Collapsed;
        ProcessesPage.Visibility = Visibility.Collapsed;
        RulesPage.Visibility = Visibility.Collapsed;
        AutomationPage.Visibility = Visibility.Collapsed;
        HistoryPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;

        targetPage.Visibility = Visibility.Visible;

        ResetNavStyle(DashboardNavButton);
        ResetNavStyle(ProcessesNavButton);
        ResetNavStyle(RulesNavButton);
        ResetNavStyle(AutomationNavButton);
        ResetNavStyle(HistoryNavButton);
        ResetNavStyle(SettingsNavButton);

        activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0E949E"));
        activeButton.Foreground = Brushes.White;
        activeButton.FontWeight = FontWeights.SemiBold;
    }

    private static void ResetNavStyle(Button button)
    {
        button.Background = Brushes.Transparent;
        button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202938"));
        button.FontWeight = FontWeights.Normal;
    }
}
