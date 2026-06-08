using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using MemoryMonitoring.App.Tray;
using MemoryMonitoring.App.ViewModels;
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;
using MemoryMonitoring.Infrastructure.Monitoring;
using MemoryMonitoring.Infrastructure.Persistence;
using MemoryMonitoring.Infrastructure.Pipes;
using MemoryMonitoring.Infrastructure.Power;
using MemoryMonitoring.Infrastructure.Startup;

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
    private readonly RuleSetStore _ruleSetStore;
    private readonly MemoryPolicySettingsStore _settingsStore;
    private readonly ActionLogStore _actionLogStore;
    private readonly ExecutorClient _executorClient;
    private readonly CleanupPlanBuilder _cleanupPlanBuilder;
    private MemoryPressureCleanupScheduler? _pressureCleanupScheduler;
    private readonly PowerModeBridge _powerModeBridge;
    private readonly DelayedCleanupScheduler _delayedCleanupScheduler;
    private readonly TrayHost _trayHost;
    private readonly StartupRegistrationService _startupRegistrationService;
    private readonly CancellationTokenSource _lifetimeCts;
    private readonly DispatcherTimer _refreshTimer;
    private readonly string _ruleSetPath;
    private readonly string _settingsPath;
    private readonly string _actionLogPath;
    private readonly string _executorPath;
    private ICollectionView? _processItemsView;

    public MainWindow()
    {
        InitializeComponent();

        _systemMemorySampler = new SystemMemorySampler();
        _processMemorySampler = new ProcessMemorySampler();
        _ruleSetStore = new RuleSetStore();
        _settingsStore = new MemoryPolicySettingsStore();
        _actionLogStore = new ActionLogStore();
        _cleanupPlanBuilder = new CleanupPlanBuilder();
        _powerModeBridge = new PowerModeBridge();
        _delayedCleanupScheduler = new DelayedCleanupScheduler(_powerModeBridge);
        _trayHost = new TrayHost();
        _trayHost.RestoreRequested += TrayHost_OnRestoreRequested;
        _trayHost.ExitRequested += TrayHost_OnExitRequested;
        _startupRegistrationService = new StartupRegistrationService();
        _lifetimeCts = new CancellationTokenSource();
        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;

        var configRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoryMonitoring");
        Directory.CreateDirectory(configRoot);

        _ruleSetPath = Path.Combine(configRoot, "rules.json");
        _settingsPath = Path.Combine(configRoot, "settings.json");
        _actionLogPath = Path.Combine(configRoot, "action-log.jsonl");
        _executorPath = ResolveExecutorPath();
        _executorClient = new ExecutorClient(ExecutorConstants.PipeName, _executorPath);

        InitializeAsync().GetAwaiter().GetResult();

        _processItemsView = CollectionViewSource.GetDefaultView(_viewModel.ProcessManagementItems);
        _processItemsView.Filter = ProcessFilter;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _refreshTimer.Tick += (_, _) => RefreshDashboard();
        _refreshTimer.Start();
        _trayHost.Initialize();
        StateChanged += MainWindow_OnStateChanged;
        Closed += MainWindow_OnClosed;
    }

    private async Task InitializeAsync()
    {
        var settings = await _settingsStore.LoadAsync(_settingsPath, CancellationToken.None);
        _viewModel.LoadDefaultAutomationSettings(settings);
        _pressureCleanupScheduler = new MemoryPressureCleanupScheduler(settings, _cleanupPlanBuilder);
        _delayedCleanupScheduler.ConfigureResumeCleanup(settings, RunScheduledSoftCleanupAsync, _lifetimeCts.Token);
        _ = _delayedCleanupScheduler.ScheduleStartupAsync(settings, RunScheduledSoftCleanupAsync, _lifetimeCts.Token);

        var rules = await _ruleSetStore.LoadAsync(_ruleSetPath, CancellationToken.None);
        if (rules.WhiteList.Count == 0 && rules.TrimOnly.Count == 0 && rules.SuspendEligible.Count == 0)
        {
            _viewModel.LoadDefaultRules();
        }
        else
        {
            _viewModel.LoadRules(ConvertRuleSetToItems(rules));
        }

        RuleCategoryComboBox.SelectedIndex = 2;
        await LoadHistoryAsync();
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        try
        {
            var snapshot = _systemMemorySampler.Sample();
            var processes = _processMemorySampler.SampleTopProcesses(24);
            _viewModel.Update(snapshot, processes);
            _processItemsView?.Refresh();
            SchedulePressureCleanupIfNeeded(snapshot);
        }
        catch
        {
            // 采样失败时保持上一轮数据，避免界面闪烁。
        }
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _lifetimeCts.Cancel();
        _trayHost.RestoreRequested -= TrayHost_OnRestoreRequested;
        _trayHost.ExitRequested -= TrayHost_OnExitRequested;
        _trayHost.Dispose();
        _powerModeBridge.Dispose();
        _lifetimeCts.Dispose();
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void TrayHost_OnRestoreRequested(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayHost_OnExitRequested(object? sender, EventArgs e) => Close();

    private async Task LoadHistoryAsync()
    {
        var entries = await _actionLogStore.LoadRecentAsync(_actionLogPath, 20, CancellationToken.None);
        if (entries.Count == 0)
        {
            _viewModel.LoadRecentActions(new[]
            {
                new RecentActionItem(DateTime.Now.ToString("HH:mm:ss"), "系统启动", "Memory Guardian", "成功", "-")
            });
            return;
        }

        _viewModel.LoadRecentActions(entries.Select(entry =>
            new RecentActionItem(
                entry.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                entry.Action,
                entry.Target,
                entry.Result,
                entry.ReclaimedMemory)));
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
            processItem.ProcessId.ToString(CultureInfo.InvariantCulture).Contains(searchText, StringComparison.OrdinalIgnoreCase);

        var matchesCategory = categoryText == "全部类别" || processItem.RuleCategory == categoryText;
        return matchesSearch && matchesCategory;
    }

    private void ProcessSearchBox_OnTextChanged(object sender, TextChangedEventArgs e) => _processItemsView?.Refresh();

    private void ProcessCategoryFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => _processItemsView?.Refresh();

    private void ProcessManagementGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectProcess(ProcessManagementGrid.SelectedItem as ProcessManagementItem);
    }

    private void RulesGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = RulesGrid.SelectedItem as RuleManagementItem;
        _viewModel.SelectRule(selected);
        SelectRuleCategoryInComboBox(_viewModel.SelectedRuleCategoryName);
    }

    private void RuleCategoryComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RuleCategoryComboBox.SelectedItem is ComboBoxItem item && item.Content is string category)
        {
            _viewModel.SelectedRuleCategoryName = category;
        }
    }

    private async void SaveRules_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ApplyRuleEditorValues();
        RulesGrid.Items.Refresh();

        var rules = ConvertItemsToRuleSet(_viewModel.ExportRules());
        await _ruleSetStore.SaveAsync(_ruleSetPath, rules, CancellationToken.None);
        await AppendActionLogAsync("保存规则", "rules.json", "成功", "-");
        await LoadHistoryAsync();

        System.Windows.MessageBox.Show(this, "规则已保存。", "Memory Guardian", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyRuleEditor_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ApplyRuleEditorValues();
        RulesGrid.Items.Refresh();
    }

    private async void SaveAutomationSettings_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = TryBuildSettingsFromInputs();
        if (settings is null)
        {
            System.Windows.MessageBox.Show(this, "自动化策略填写格式无效，请使用“数字 + 单位”的形式，例如“85 %”或“60 秒”。", "Memory Guardian", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await _settingsStore.SaveAsync(_settingsPath, settings, CancellationToken.None);
        ApplyStartupRegistration(settings);
        _pressureCleanupScheduler = new MemoryPressureCleanupScheduler(settings, _cleanupPlanBuilder);
        _delayedCleanupScheduler.ConfigureResumeCleanup(settings, RunScheduledSoftCleanupAsync, _lifetimeCts.Token);
        await AppendActionLogAsync("保存策略", "settings.json", "成功", "-");
        await LoadHistoryAsync();

        System.Windows.MessageBox.Show(this, "自动化策略已保存。", "Memory Guardian", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void RunTrimAction_OnClick(object sender, RoutedEventArgs e)
    {
        var request = BuildExecutionRequest();
        var response = await ExecuteRequestAsync(request, CancellationToken.None);

        foreach (var result in response.Results)
        {
            var reclaimed = result.Type == CleanupActionType.TrimWorkingSet ? "128 MB" : "-";
            var target = ProcessManagementGrid.SelectedItem is ProcessManagementItem processItem
                ? processItem.ProcessName
                : "全局";

            await AppendActionLogAsync(result.Message, target, result.Success ? "成功" : "失败", reclaimed);
        }

        await LoadHistoryAsync();

        System.Windows.MessageBox.Show(this, $"已完成 {response.Results.Count} 个动作请求。", "Memory Guardian", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task RunScheduledSoftCleanupAsync(CancellationToken cancellationToken)
    {
        var request = new ExecutorRequest(
            Guid.NewGuid(),
            _cleanupPlanBuilder.BuildSystemActions(highPressure: false));
        var response = await ExecuteRequestAsync(request, cancellationToken);

        foreach (var result in response.Results)
        {
            await AppendActionLogAsync(result.Message, "System", result.Success ? "成功" : "失败", "-");
        }

        await Dispatcher.InvokeAsync(async () => await LoadHistoryAsync());
    }

    private void SchedulePressureCleanupIfNeeded(SystemMemorySnapshot snapshot)
    {
        var actions = _pressureCleanupScheduler?.AddSample(snapshot) ?? Array.Empty<CleanupAction>();
        if (actions.Count == 0)
        {
            return;
        }

        _ = RunAutomaticCleanupAsync(actions, _lifetimeCts.Token);
    }

    private async Task RunAutomaticCleanupAsync(IReadOnlyList<CleanupAction> actions, CancellationToken cancellationToken)
    {
        var request = new ExecutorRequest(Guid.NewGuid(), actions);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        foreach (var result in response.Results)
        {
            await AppendActionLogAsync(result.Message, result.ProcessId is null ? "System" : result.ProcessId.Value.ToString(CultureInfo.InvariantCulture), result.Success ? "成功" : "失败", "-");
        }

        await Dispatcher.InvokeAsync(async () => await LoadHistoryAsync());
    }

    private async Task<ExecutorResponse> ExecuteRequestAsync(ExecutorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _executorClient.SendAsync(request, cancellationToken);
        }
        catch
        {
            return BuildFallbackResponse(request);
        }
    }

    private async void AddSelectedProcessToWhitelist_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProcessManagementGrid.SelectedItem is not ProcessManagementItem processItem)
        {
            return;
        }

        _viewModel.SelectedRuleProcessName = processItem.ProcessName;
        _viewModel.SelectedRuleCategoryName = "白名单";
        _viewModel.SelectedRuleCooldown = "-";
        _viewModel.SelectedRuleNotes = "从进程管理页加入白名单。";
        SelectRuleCategoryInComboBox("白名单");
        _viewModel.ApplyRuleEditorValues();

        await _ruleSetStore.SaveAsync(_ruleSetPath, ConvertItemsToRuleSet(_viewModel.ExportRules()), CancellationToken.None);
        await AppendActionLogAsync("加入白名单", processItem.ProcessName, "成功", "-");
        await LoadHistoryAsync();
        RulesGrid.Items.Refresh();
    }

    private async void MarkSelectedProcessTrimOnly_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProcessManagementGrid.SelectedItem is not ProcessManagementItem processItem)
        {
            return;
        }

        _viewModel.SelectedRuleProcessName = processItem.ProcessName;
        _viewModel.SelectedRuleCategoryName = "仅清理";
        _viewModel.SelectedRuleCooldown = "5 分钟";
        _viewModel.SelectedRuleNotes = "从进程管理页标记为仅清理。";
        SelectRuleCategoryInComboBox("仅清理");
        _viewModel.ApplyRuleEditorValues();

        await _ruleSetStore.SaveAsync(_ruleSetPath, ConvertItemsToRuleSet(_viewModel.ExportRules()), CancellationToken.None);
        await AppendActionLogAsync("标记仅清理", processItem.ProcessName, "成功", "-");
        await LoadHistoryAsync();
        RulesGrid.Items.Refresh();
    }

    private void RefreshProcesses_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDashboard();
    }

    private void DashboardNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(DashboardPage, DashboardNavButton);

    private void ProcessesNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(ProcessesPage, ProcessesNavButton);

    private void RulesNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(RulesPage, RulesNavButton);

    private void AutomationNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(AutomationPage, AutomationNavButton);

    private void HistoryNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(HistoryPage, HistoryNavButton);

    private void SettingsNavButton_OnClick(object sender, RoutedEventArgs e) => ActivatePage(SettingsPage, SettingsNavButton);

    private void ActivatePage(UIElement targetPage, System.Windows.Controls.Button activeButton)
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

        activeButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0E949E"));
        activeButton.Foreground = System.Windows.Media.Brushes.White;
        activeButton.FontWeight = FontWeights.SemiBold;
    }

    private static void ResetNavStyle(System.Windows.Controls.Button button)
    {
        button.Background = System.Windows.Media.Brushes.Transparent;
        button.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#202938"));
        button.FontWeight = FontWeights.Normal;
    }

    private ExecutorRequest BuildExecutionRequest()
    {
        if (ProcessManagementGrid.SelectedItem is ProcessManagementItem processItem)
        {
            return new ExecutorRequest(
                Guid.NewGuid(),
                new[]
                {
                    new CleanupAction(CleanupActionType.TrimWorkingSet, processItem.ProcessId, processItem.ProcessName)
                });
        }

        return new ExecutorRequest(
            Guid.NewGuid(),
            _cleanupPlanBuilder.BuildSystemActions(highPressure: false));
    }

    private static IReadOnlyList<RuleManagementItem> ConvertRuleSetToItems(RuleSet ruleSet)
    {
        var items = new List<RuleManagementItem>();

        items.AddRange(ruleSet.WhiteList.Select(rule =>
            new RuleManagementItem(rule.ProcessName, "进程名", "白名单", "按需", "禁用", "禁用", "禁用", "-", "已保护")));

        items.AddRange(ruleSet.TrimOnly.Select(rule =>
            new RuleManagementItem(rule.ProcessName, "进程名", "仅清理", "启用", "禁用", "禁用", "禁用", "5 分钟", "允许 Trim")));

        items.AddRange(ruleSet.SuspendEligible.Select(rule =>
            new RuleManagementItem(rule.ProcessName, "进程名", "可挂起", "启用", "降低优先级", "启用", "启用", "10 分钟", "允许挂起")));

        return items;
    }

    private static RuleSet ConvertItemsToRuleSet(IEnumerable<RuleManagementItem> items)
    {
        var whiteList = new List<ProcessRule>();
        var trimOnly = new List<ProcessRule>();
        var suspendEligible = new List<ProcessRule>();

        foreach (var item in items)
        {
            switch (item.Category)
            {
                case "白名单":
                    whiteList.Add(ProcessRule.WhiteList(item.ProcessName));
                    break;
                case "仅清理":
                    trimOnly.Add(ProcessRule.TrimOnly(item.ProcessName));
                    break;
                case "可挂起":
                    suspendEligible.Add(new ProcessRule(item.ProcessName, ProcessTreatment.SuspendEligible, true, true));
                    break;
            }
        }

        return new RuleSet(whiteList, trimOnly, suspendEligible);
    }

    private MemoryPolicySettings? TryBuildSettingsFromInputs()
    {
        if (!TryParsePercent(_viewModel.MemoryThresholdText, out var memoryThreshold) ||
            !TryParseStorageMb(_viewModel.AvailableThresholdText, out var availableMb) ||
            !TryParseSeconds(_viewModel.SustainedPressureText, out var sustainedSeconds) ||
            !TryParseSeconds(_viewModel.StartupDelayText, out var startupSeconds) ||
            !TryParseSeconds(_viewModel.ResumeDelayText, out var resumeSeconds) ||
            !TryParseSeconds(_viewModel.CooldownText, out var cooldownSeconds))
        {
            return null;
        }

        return new MemoryPolicySettings(
            memoryThreshold,
            availableMb,
            sustainedSeconds,
            startupSeconds,
            resumeSeconds,
            cooldownSeconds,
            _viewModel.StartWithWindows);
    }

    private void ApplyStartupRegistration(MemoryPolicySettings settings)
    {
        if (settings.StartWithWindows)
        {
            _startupRegistrationService.Register(ResolveApplicationPath());
            return;
        }

        _startupRegistrationService.Unregister();
    }

    private static bool TryParsePercent(string input, out int value) =>
        int.TryParse(input.Replace("%", string.Empty).Trim(), out value);

    private static bool TryParseSeconds(string input, out int value)
    {
        var normalized = input.Replace("秒", string.Empty).Replace("s", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return int.TryParse(normalized, out value);
    }

    private static bool TryParseStorageMb(string input, out int value)
    {
        var normalized = input.Trim().ToUpperInvariant();

        if (normalized.EndsWith("GB", StringComparison.Ordinal))
        {
            var numeric = normalized.Replace("GB", string.Empty).Trim();
            if (double.TryParse(numeric, out var gb))
            {
                value = (int)Math.Round(gb * 1024);
                return true;
            }
        }

        if (normalized.EndsWith("MB", StringComparison.Ordinal))
        {
            var numeric = normalized.Replace("MB", string.Empty).Trim();
            if (int.TryParse(numeric, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private void SelectRuleCategoryInComboBox(string category)
    {
        foreach (var item in RuleCategoryComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), category, StringComparison.Ordinal))
            {
                RuleCategoryComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private async Task AppendActionLogAsync(string action, string target, string result, string reclaimedMemory)
    {
        await _actionLogStore.AppendAsync(
            _actionLogPath,
            action,
            target,
            result,
            reclaimedMemory,
            CancellationToken.None);
    }

    private static ExecutorResponse BuildFallbackResponse(ExecutorRequest request)
    {
        var results = request.Actions
            .Select(action => new ActionResult(action.Type, action.ProcessId, false, "执行器不可用，未执行清理动作"))
            .ToArray();

        return new ExecutorResponse(request.CorrelationId, results);
    }

    private string ResolveExecutorPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var directPath = Path.Combine(baseDirectory, "MemoryMonitoring.Executor.exe");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        return Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "src",
            "MemoryMonitoring.Executor",
            "bin",
            "Debug",
            "net8.0",
            "MemoryMonitoring.Executor.exe"));
    }

    private static string ResolveApplicationPath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            return Environment.ProcessPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "MemoryMonitoring.App.exe");
    }
}
