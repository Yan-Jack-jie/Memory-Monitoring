using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.App.ViewModels;

/// <summary>
/// 仪表盘页的基础视图模型。
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public DashboardViewModel()
    {
        TopProcesses = new ObservableCollection<ProcessOverviewItem>();
        RecentActions = new ObservableCollection<RecentActionItem>();
    }

    public ObservableCollection<ProcessOverviewItem> TopProcesses { get; }

    public ObservableCollection<RecentActionItem> RecentActions { get; }

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

            foreach (var process in processSnapshots)
            {
                TopProcesses.Add(new ProcessOverviewItem(
                    Process: process.ProcessName,
                    Pid: process.ProcessId,
                    WorkingSet: FormatBytes(process.WorkingSetBytes),
                    PrivateBytes: FormatBytes(process.PrivateBytes),
                    Rule: "自动清理",
                    LastActive: process.LastForegroundSeenAt.ToString("HH:mm:ss"),
                    Action: "Trim"));
            }
        }

        if (RecentActions.Count == 0)
        {
            RecentActions.Add(new RecentActionItem(DateTime.Now.ToString("HH:mm:ss"), "系统启动", "Memory Guardian", "成功", "-"));
        }
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
