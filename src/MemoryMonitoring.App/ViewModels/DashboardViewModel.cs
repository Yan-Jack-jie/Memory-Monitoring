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

    public event PropertyChangedEventHandler? PropertyChanged;

    public string MemoryLoadText
    {
        get => _memoryLoadText;
        private set
        {
            _memoryLoadText = value;
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

    public void Update(SystemMemorySnapshot snapshot)
    {
        MemoryLoadText = $"{snapshot.MemoryLoadPercent}%";
        AvailableMemoryText = $"{snapshot.AvailableMemoryMb} MB";
        CommitUsageText = $"{snapshot.CommitUsedMb / 1024d:F1} GB";
        CacheUsageText = $"{snapshot.SystemCacheMb / 1024d:F1} GB";
        StatusText = snapshot.MemoryLoadPercent >= 85 ? "告急" : snapshot.MemoryLoadPercent >= 65 ? "警戒" : "健康";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
