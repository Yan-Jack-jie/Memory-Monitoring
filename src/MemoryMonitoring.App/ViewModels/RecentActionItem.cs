namespace MemoryMonitoring.App.ViewModels;

/// <summary>
/// 仪表盘最近动作表行模型。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record RecentActionItem(
    string Time,
    string Type,
    string Target,
    string Result,
    string Reclaimed);
