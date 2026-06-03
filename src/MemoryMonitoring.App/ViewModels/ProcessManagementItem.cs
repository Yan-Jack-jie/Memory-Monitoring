namespace MemoryMonitoring.App.ViewModels;

/// <summary>
/// 进程管理页的数据行模型。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ProcessManagementItem(
    string ProcessName,
    int ProcessId,
    string MemoryUsage,
    string PrivateMemory,
    string LastActive,
    string RuleCategory,
    string RecommendedAction,
    string ManualActions);
