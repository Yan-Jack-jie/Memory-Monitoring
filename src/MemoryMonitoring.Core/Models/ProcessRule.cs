namespace MemoryMonitoring.Core.Models;

/// <summary>
/// 描述单个进程的自动化处理规则。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed record ProcessRule(
    string ProcessName,
    ProcessTreatment Treatment,
    bool AllowTrim,
    bool AllowSuspend)
{
    public static ProcessRule WhiteList(string processName) =>
        new(processName, ProcessTreatment.WhiteList, AllowTrim: true, AllowSuspend: false);

    public static ProcessRule TrimOnly(string processName) =>
        new(processName, ProcessTreatment.TrimOnly, AllowTrim: true, AllowSuspend: false);
}
