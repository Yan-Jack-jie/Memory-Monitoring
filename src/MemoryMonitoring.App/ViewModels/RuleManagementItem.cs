namespace MemoryMonitoring.App.ViewModels;

/// <summary>
/// 规则管理页的数据行模型。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record RuleManagementItem(
    string ProcessName,
    string MatchType,
    string Category,
    string AutoTrim,
    string PriorityMode,
    string PowerThrottling,
    string AutoSuspend,
    string Cooldown,
    string Notes);
