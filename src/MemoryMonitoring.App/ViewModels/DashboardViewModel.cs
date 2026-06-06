using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.App.ViewModels;

/// <summary>
/// 主界面共享视图模型。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private const int MemoryHistoryCapacity = 120;
    private string _memoryLoadText = "0%";
    private string _availableMemoryText = "0 MB";
    private string _commitUsageText = "0 GB";
    private string _cacheUsageText = "0 GB";
    private string _statusText = "健康";
    private string _automationStatusText = "运行中";
    private double _memoryLoadValue;
    private string _memorySubtitleText = "-";
    private string _whiteListCountText = "0";
    private string _trimOnlyCountText = "0";
    private string _balancedCountText = "0";
    private string _suspendEligibleCountText = "0";
    private string _memoryThresholdText = "85 %";
    private string _availableThresholdText = "2.0 GB";
    private string _sustainedPressureText = "45 秒";
    private string _startupDelayText = "90 秒";
    private string _resumeDelayText = "60 秒";
    private string _cooldownText = "180 秒";
    private string _selectedProcessName = "未选择进程";
    private string _selectedProcessHint = "请选择左侧列表中的进程查看详情。";
    private string _selectedRuleCategory = "-";
    private string _selectedRecommendedAction = "-";
    private string _selectedProcessMemory = "-";
    private string _selectedProcessPrivateMemory = "-";
    private string _selectedRuleProcessName = "chrome.exe";
    private string _selectedRuleCategoryName = "平衡";
    private string _selectedRuleCooldown = "5 分钟";
    private string _selectedRuleNotes = "浏览器，允许降级和 Trim，避免直接挂起。";
    private string _historyTotalActionsText = "0";
    private string _historySuccessCountText = "0";
    private string _historyLastActionText = "-";

    public event PropertyChangedEventHandler? PropertyChanged;

    public DashboardViewModel()
    {
        TopProcesses = new ObservableCollection<ProcessOverviewItem>();
        RecentActions = new ObservableCollection<RecentActionItem>();
        ProcessManagementItems = new ObservableCollection<ProcessManagementItem>();
        RuleItems = new ObservableCollection<RuleManagementItem>();
        MemoryHistory = new ObservableCollection<MemoryHistoryPoint>();
    }

    public ObservableCollection<ProcessOverviewItem> TopProcesses { get; }

    public ObservableCollection<RecentActionItem> RecentActions { get; }

    public ObservableCollection<ProcessManagementItem> ProcessManagementItems { get; }

    public ObservableCollection<RuleManagementItem> RuleItems { get; }

    public ObservableCollection<MemoryHistoryPoint> MemoryHistory { get; }

    public string MemoryLoadText
    {
        get => _memoryLoadText;
        private set => SetField(ref _memoryLoadText, value);
    }

    public double MemoryLoadValue
    {
        get => _memoryLoadValue;
        private set => SetField(ref _memoryLoadValue, value);
    }

    public string MemorySubtitleText
    {
        get => _memorySubtitleText;
        private set => SetField(ref _memorySubtitleText, value);
    }

    public string AvailableMemoryText
    {
        get => _availableMemoryText;
        private set => SetField(ref _availableMemoryText, value);
    }

    public string CommitUsageText
    {
        get => _commitUsageText;
        private set => SetField(ref _commitUsageText, value);
    }

    public string CacheUsageText
    {
        get => _cacheUsageText;
        private set => SetField(ref _cacheUsageText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string AutomationStatusText
    {
        get => _automationStatusText;
        private set => SetField(ref _automationStatusText, value);
    }

    public string WhiteListCountText
    {
        get => _whiteListCountText;
        private set => SetField(ref _whiteListCountText, value);
    }

    public string TrimOnlyCountText
    {
        get => _trimOnlyCountText;
        private set => SetField(ref _trimOnlyCountText, value);
    }

    public string BalancedCountText
    {
        get => _balancedCountText;
        private set => SetField(ref _balancedCountText, value);
    }

    public string SuspendEligibleCountText
    {
        get => _suspendEligibleCountText;
        private set => SetField(ref _suspendEligibleCountText, value);
    }

    public string MemoryThresholdText
    {
        get => _memoryThresholdText;
        set => SetField(ref _memoryThresholdText, value);
    }

    public string AvailableThresholdText
    {
        get => _availableThresholdText;
        set => SetField(ref _availableThresholdText, value);
    }

    public string SustainedPressureText
    {
        get => _sustainedPressureText;
        set => SetField(ref _sustainedPressureText, value);
    }

    public string StartupDelayText
    {
        get => _startupDelayText;
        set => SetField(ref _startupDelayText, value);
    }

    public string ResumeDelayText
    {
        get => _resumeDelayText;
        set => SetField(ref _resumeDelayText, value);
    }

    public string CooldownText
    {
        get => _cooldownText;
        set => SetField(ref _cooldownText, value);
    }

    public string SelectedProcessName
    {
        get => _selectedProcessName;
        private set => SetField(ref _selectedProcessName, value);
    }

    public string SelectedProcessHint
    {
        get => _selectedProcessHint;
        private set => SetField(ref _selectedProcessHint, value);
    }

    public string SelectedRuleCategory
    {
        get => _selectedRuleCategory;
        private set => SetField(ref _selectedRuleCategory, value);
    }

    public string SelectedRecommendedAction
    {
        get => _selectedRecommendedAction;
        private set => SetField(ref _selectedRecommendedAction, value);
    }

    public string SelectedProcessMemory
    {
        get => _selectedProcessMemory;
        private set => SetField(ref _selectedProcessMemory, value);
    }

    public string SelectedProcessPrivateMemory
    {
        get => _selectedProcessPrivateMemory;
        private set => SetField(ref _selectedProcessPrivateMemory, value);
    }

    public string SelectedRuleProcessName
    {
        get => _selectedRuleProcessName;
        set => SetField(ref _selectedRuleProcessName, value);
    }

    public string SelectedRuleCategoryName
    {
        get => _selectedRuleCategoryName;
        set => SetField(ref _selectedRuleCategoryName, value);
    }

    public string SelectedRuleCooldown
    {
        get => _selectedRuleCooldown;
        set => SetField(ref _selectedRuleCooldown, value);
    }

    public string SelectedRuleNotes
    {
        get => _selectedRuleNotes;
        set => SetField(ref _selectedRuleNotes, value);
    }

    public string HistoryTotalActionsText
    {
        get => _historyTotalActionsText;
        private set => SetField(ref _historyTotalActionsText, value);
    }

    public string HistorySuccessCountText
    {
        get => _historySuccessCountText;
        private set => SetField(ref _historySuccessCountText, value);
    }

    public string HistoryLastActionText
    {
        get => _historyLastActionText;
        private set => SetField(ref _historyLastActionText, value);
    }

    public void Update(SystemMemorySnapshot snapshot, IReadOnlyList<ProcessMemorySnapshot>? processSnapshots = null)
    {
        MemoryLoadText = $"{snapshot.MemoryLoadPercent}%";
        MemoryLoadValue = snapshot.MemoryLoadPercent;
        AvailableMemoryText = FormatMb(snapshot.AvailableMemoryMb);
        CommitUsageText = $"{snapshot.CommitUsedMb / 1024d:F1} GB";
        CacheUsageText = $"{snapshot.SystemCacheMb / 1024d:F1} GB";
        StatusText = snapshot.MemoryLoadPercent >= 85 ? "告急" : snapshot.MemoryLoadPercent >= 65 ? "警戒" : "健康";
        MemorySubtitleText = $"可用物理内存 {snapshot.AvailableMemoryMb / 1024d:F1} GB";

        AppendMemoryHistory(snapshot);

        if (processSnapshots is not null)
        {
            TopProcesses.Clear();
            ProcessManagementItems.Clear();

            foreach (var process in processSnapshots)
            {
                var category = InferRuleCategory(process.ProcessName);
                var lastActive = process.LastForegroundSeenAt.ToString("HH:mm:ss");
                var workingSet = FormatBytes(process.WorkingSetBytes);
                var privateBytes = FormatBytes(process.PrivateBytes);

                TopProcesses.Add(new ProcessOverviewItem(
                    process.ProcessName,
                    process.ProcessId,
                    workingSet,
                    privateBytes,
                    category,
                    lastActive,
                    "Trim"));

                ProcessManagementItems.Add(new ProcessManagementItem(
                    process.ProcessName,
                    process.ProcessId,
                    workingSet,
                    privateBytes,
                    lastActive,
                    category,
                    category == "白名单" ? "无须操作" : "Trim 工作集",
                    category == "白名单" ? "查看详情" : "Trim / 调优"));
            }
        }

        if (RecentActions.Count == 0)
        {
            RecentActions.Add(new RecentActionItem(DateTime.Now.ToString("HH:mm:ss"), "系统启动", "Memory Guardian", "成功", "-"));
        }
    }

    public void LoadRules(IEnumerable<RuleManagementItem> items)
    {
        RuleItems.Clear();

        foreach (var item in items)
        {
            RuleItems.Add(item);
        }

        UpdateRuleCounts();
    }

    public void LoadDefaultRules()
    {
        if (RuleItems.Count > 0)
        {
            return;
        }

        LoadRules(new[]
        {
            new RuleManagementItem("WeChat.exe", "进程名", "白名单", "按需", "禁用", "禁用", "禁用", "-", "即时通讯"),
            new RuleManagementItem("SecureLineVPN.exe", "进程名", "白名单", "按需", "禁用", "禁用", "禁用", "-", "VPN 连接"),
            new RuleManagementItem("chrome.exe", "进程名", "平衡", "启用", "降低优先级", "启用", "禁用", "5 分钟", "浏览器"),
            new RuleManagementItem("Cherry Studio.exe", "进程名", "仅清理", "启用", "禁用", "禁用", "禁用", "5 分钟", "翻译工具"),
            new RuleManagementItem("Everything.exe", "进程名", "可挂起", "启用", "降低优先级", "启用", "启用", "10 分钟", "后台工具")
        });
    }

    public void LoadDefaultAutomationSettings(MemoryPolicySettings settings)
    {
        MemoryThresholdText = $"{settings.MemoryLoadPercentThreshold} %";
        AvailableThresholdText = $"{settings.AvailableMemoryThresholdMb / 1024d:F1} GB";
        SustainedPressureText = $"{settings.SustainedPressureSeconds} 秒";
        StartupDelayText = $"{settings.StartupDelaySeconds} 秒";
        ResumeDelayText = $"{settings.ResumeDelaySeconds} 秒";
        CooldownText = $"{settings.GlobalCooldownSeconds} 秒";
    }

    public void SelectProcess(ProcessManagementItem? processItem)
    {
        if (processItem is null)
        {
            SelectedProcessName = "未选择进程";
            SelectedProcessHint = "请选择左侧列表中的进程查看详情。";
            SelectedRuleCategory = "-";
            SelectedRecommendedAction = "-";
            SelectedProcessMemory = "-";
            SelectedProcessPrivateMemory = "-";
            return;
        }

        SelectedProcessName = $"{processItem.ProcessName} ({processItem.ProcessId})";
        SelectedProcessHint = $"最近活动：{processItem.LastActive}";
        SelectedRuleCategory = processItem.RuleCategory;
        SelectedRecommendedAction = processItem.RecommendedAction;
        SelectedProcessMemory = processItem.MemoryUsage;
        SelectedProcessPrivateMemory = processItem.PrivateMemory;
    }

    public void SelectRule(RuleManagementItem? ruleItem)
    {
        if (ruleItem is null)
        {
            return;
        }

        SelectedRuleProcessName = ruleItem.ProcessName;
        SelectedRuleCategoryName = ruleItem.Category;
        SelectedRuleCooldown = ruleItem.Cooldown;
        SelectedRuleNotes = ruleItem.Notes;
    }

    public void ApplyRuleEditorValues()
    {
        var existing = RuleItems.FirstOrDefault(item => string.Equals(item.ProcessName, SelectedRuleProcessName, StringComparison.OrdinalIgnoreCase));
        var newItem = new RuleManagementItem(
            SelectedRuleProcessName,
            "进程名",
            SelectedRuleCategoryName,
            "启用",
            SelectedRuleCategoryName == "白名单" ? "禁用" : "降低优先级",
            SelectedRuleCategoryName is "白名单" or "仅清理" ? "禁用" : "启用",
            SelectedRuleCategoryName == "可挂起" ? "启用" : "禁用",
            SelectedRuleCooldown,
            SelectedRuleNotes);

        if (existing is null)
        {
            RuleItems.Add(newItem);
        }
        else
        {
            var index = RuleItems.IndexOf(existing);
            RuleItems[index] = newItem;
        }

        UpdateRuleCounts();
    }

    public IEnumerable<RuleManagementItem> ExportRules() => RuleItems.ToArray();

    public void LoadRecentActions(IEnumerable<RecentActionItem> items)
    {
        RecentActions.Clear();

        foreach (var item in items)
        {
            RecentActions.Add(item);
        }

        HistoryTotalActionsText = RecentActions.Count.ToString();
        HistorySuccessCountText = RecentActions.Count(item => item.Result == "成功").ToString();
        HistoryLastActionText = RecentActions.FirstOrDefault()?.Type ?? "-";
    }

    private void UpdateRuleCounts()
    {
        WhiteListCountText = RuleItems.Count(item => item.Category == "白名单").ToString();
        TrimOnlyCountText = RuleItems.Count(item => item.Category == "仅清理").ToString();
        BalancedCountText = RuleItems.Count(item => item.Category == "平衡").ToString();
        SuspendEligibleCountText = RuleItems.Count(item => item.Category == "可挂起").ToString();
    }

    private void AppendMemoryHistory(SystemMemorySnapshot snapshot)
    {
        MemoryHistory.Add(new MemoryHistoryPoint(
            snapshot.Timestamp,
            snapshot.MemoryLoadPercent,
            snapshot.AvailableMemoryMb,
            snapshot.CommitUsedMb));

        while (MemoryHistory.Count > MemoryHistoryCapacity)
        {
            MemoryHistory.RemoveAt(0);
        }
    }

    private static string InferRuleCategory(string processName)
    {
        var normalizedName = processName.ToLowerInvariant();

        if (normalizedName.Contains("vpn"))
        {
            return "白名单";
        }

        if (normalizedName.Contains("wechat") || normalizedName.Contains("cherry"))
        {
            return "仅清理";
        }

        if (normalizedName.Contains("chrome") || normalizedName.Contains("edge"))
        {
            return "平衡";
        }

        return "自动清理";
    }

    private static string FormatMb(long mb) => mb >= 1024 ? $"{mb / 1024d:F1} GB" : $"{mb} MB";

    private static string FormatBytes(long bytes)
    {
        var mb = bytes / 1024d / 1024d;
        return mb >= 1024 ? $"{mb / 1024d:F2} GB" : $"{mb:F0} MB";
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
