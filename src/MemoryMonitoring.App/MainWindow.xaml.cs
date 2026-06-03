using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using MemoryMonitoring.App.ViewModels;
using MemoryMonitoring.Core.Models;
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
    private ICollectionView? _processItemsView;

    public MainWindow()
    {
        InitializeComponent();

        _systemMemorySampler = new SystemMemorySampler();
        _processMemorySampler = new ProcessMemorySampler();
        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;

        _viewModel.LoadDefaultRules();
        _viewModel.LoadDefaultAutomationSettings(MemoryPolicySettings.CreateDefault());

        RefreshDashboard();

        _processItemsView = CollectionViewSource.GetDefaultView(_viewModel.ProcessManagementItems);
        _processItemsView.Filter = ProcessFilter;

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
            var processes = _processMemorySampler.SampleTopProcesses(24);
            _viewModel.Update(snapshot, processes);
            _processItemsView?.Refresh();
        }
        catch
        {
            // 采样失败时保持上一轮数据，避免界面闪烁。
        }
    }

    private bool ProcessFilter(object item)
    {
        if (item is not ProcessManagementItem processItem)
        {
            return false;
        }

        var searchText = ProcessSearchBox?.Text?.Trim() ?? string.Empty;
        var categoryText = (ProcessCategoryFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "全部类别";

        var matchesSearch =
            string.IsNullOrWhiteSpace(searchText) ||
            processItem.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            processItem.ProcessId.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase);

        var matchesCategory = categoryText == "全部类别" || processItem.RuleCategory == categoryText;

        return matchesSearch && matchesCategory;
    }

    private void ProcessSearchBox_OnTextChanged(object sender, TextChangedEventArgs e) => _processItemsView?.Refresh();

    private void ProcessCategoryFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => _processItemsView?.Refresh();

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
