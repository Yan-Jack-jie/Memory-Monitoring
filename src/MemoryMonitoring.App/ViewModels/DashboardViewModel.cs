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

    public event PropertyChangedEventHandler? PropertyChanged;

    public DashboardViewModel()
    {
        TopProcesses = new ObservableCollection<ProcessOverviewItem>();
        RecentActions = new ObservableCollection<RecentActionItem>();
        ProcessManagementItems = new ObservableCollection<ProcessManagementItem>();
        RuleItems = new ObservableCollection<RuleManagementItem>();
    }

    public ObservableCollection<ProcessOverviewItem> TopProcesses { get; }

    public ObservableCollection<RecentActionItem> RecentActions { get; }

    public ObservableCollection<ProcessManagementItem> ProcessManagementItems { get; }

    public ObservableCollection<RuleManagementItem> RuleItems { get; }

    public string MemoryLoadText
    {
        get => _memoryLoadText;
        private set
        {
            _memoryLoadText = value;
            OnPropertyChanged();
        }
    }

    public double MemoryLoadValue
    {
        get => _memoryLoadValue;
        private set
        {
            _memoryLoadValue = value;
            OnPropertyChanged();
        }
    }

    public string MemorySubtitleText
    {
        get => _memorySubtitleText;
        private set
        {
            _memorySubtitleText = value;
            OnPropertyChanged();
        }
    }

    public string AvailableMemoryText
    {
        get => _availableMemoryText;
        private set
        {
            _availableMemoryText = value;
            OnPropertyChanged();
        }
    }

    public string CommitUsageText
    {
        get => _commitUsageText;
        private set
        {
            _commitUsageText = value;
            OnPropertyChanged();
        }
    }

    public string CacheUsageText
    {
        get => _cacheUsageText;
        private set
        {
            _cacheUsageText = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string AutomationStatusText
    {
        get => _automationStatusText;
        private set
        {
            _automationStatusText = value;
            OnPropertyChanged();
        }
    }

    public string WhiteListCountText
    {
        get => _whiteListCountText;
        private set
        {
            _whiteListCountText = value;
            OnPropertyChanged();
        }
    }

    public string TrimOnlyCountText
    {
        get => _trimOnlyCountText;
        private set
        {
            _trimOnlyCountText = value;
            OnPropertyChanged();
        }
    }

    public string BalancedCountText
    {
        get => _balancedCountText;
        private set
        {
            _balancedCountText = value;
            OnPropertyChanged();
        }
    }

    public string SuspendEligibleCountText
    {
        get => _suspendEligibleCountText;
        private set
        {
            _suspendEligibleCountText = value;
            OnPropertyChanged();
        }
    }

    public string MemoryThresholdText
    {
        get => _memoryThresholdText;
        private set
        {
            _memoryThresholdText = value;
            OnPropertyChanged();
        }
    }

    public string AvailableThresholdText
    {
        get => _availableThresholdText;
        private set
        {
            _availableThresholdText = value;
            OnPropertyChanged();
        }
    }

    public string SustainedPressureText
    {
        get => _sustainedPressureText;
        private set
        {
            _sustainedPressureText = value;
            OnPropertyChanged();
        }
    }

    public string StartupDelayText
    {
        get => _startupDelayText;
        private set
        {
            _startupDelayText = value;
            OnPropertyChanged();
        }
    }

    public string ResumeDelayText
    {
        get => _resumeDelayText;
        private set
        {
            _resumeDelayText = value;
            OnPropertyChanged();
        }
    }

    public string CooldownText
    {
        get => _cooldownText;
        private set
        {
            _cooldownText = value;
            OnPropertyChanged();
        }
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

        if (processSnapshots is not null)
        {
            TopProcesses.Clear();
            ProcessManagementItems.Clear();

            foreach (var process in processSnapshots)
            {
                var category = InferRuleCategory(process.ProcessName);
                var lastActive = process.LastForegroundSeenAt.ToString("HH:mm:ss");

                TopProcesses.Add(new ProcessOverviewItem(
                    Process: process.ProcessName,
                    Pid: process.ProcessId,
                    WorkingSet: FormatBytes(process.WorkingSetBytes),
                    PrivateBytes: FormatBytes(process.PrivateBytes),
                    Rule: category,
                    LastActive: lastActive,
                    Action: "Trim"));

                ProcessManagementItems.Add(new ProcessManagementItem(
                    ProcessName: process.ProcessName,
                    ProcessId: process.ProcessId,
                    MemoryUsage: FormatBytes(process.WorkingSetBytes),
                    PrivateMemory: FormatBytes(process.PrivateBytes),
                    LastActive: lastActive,
                    RuleCategory: category,
                    RecommendedAction: category == "白名单" ? "无须操作" : "Trim 工作集",
                    ManualActions: category == "白名单" ? "查看详情" : "Trim / 调优"));
            }
        }

        if (RecentActions.Count == 0)
        {
            RecentActions.Add(new RecentActionItem(DateTime.Now.ToString("HH:mm:ss"), "系统启动", "Memory Guardian", "成功", "-"));
        }
    }

    public void LoadDefaultRules()
    {
        if (RuleItems.Count > 0)
        {
            return;
        }

        RuleItems.Add(new RuleManagementItem("WeChat.exe", "进程名", "白名单", "按需", "禁用", "禁用", "禁用", "-", "即时通讯"));
        RuleItems.Add(new RuleManagementItem("SecureLineVPN.exe", "进程名", "白名单", "按需", "禁用", "禁用", "禁用", "-", "VPN 连接"));
        RuleItems.Add(new RuleManagementItem("chrome.exe", "进程名", "平衡", "启用", "降低优先级", "启用", "禁用", "5 分钟", "浏览器"));
        RuleItems.Add(new RuleManagementItem("Cherry Studio.exe", "进程名", "仅清理", "启用", "禁用", "禁用", "禁用", "5 分钟", "翻译工具"));
        RuleItems.Add(new RuleManagementItem("Everything.exe", "进程名", "可挂起", "启用", "降低优先级", "启用", "启用", "10 分钟", "后台工具"));

        WhiteListCountText = RuleItems.Count(item => item.Category == "白名单").ToString();
        TrimOnlyCountText = RuleItems.Count(item => item.Category == "仅清理").ToString();
        BalancedCountText = RuleItems.Count(item => item.Category == "平衡").ToString();
        SuspendEligibleCountText = RuleItems.Count(item => item.Category == "可挂起").ToString();
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
