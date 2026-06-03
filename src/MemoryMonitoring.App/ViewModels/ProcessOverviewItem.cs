namespace MemoryMonitoring.App.ViewModels;

/// <summary>
/// 仪表盘进程表行模型。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ProcessOverviewItem(
    string Process,
    int Pid,
    string WorkingSet,
    string PrivateBytes,
    string Rule,
    string LastActive,
    string Action);
