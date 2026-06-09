using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Interop;

namespace MemoryMonitoring.Tests.Core;

public sealed class NativeMemoryActionsTests
{
    [Fact]
    public void ProcessAction_ShouldFailWhenProcessIdIsMissing()
    {
        var actions = new NativeMemoryActions();

        var result = actions.Execute(new CleanupAction(
            CleanupActionType.TrimWorkingSet,
            ProcessId: null,
            ProcessName: "missing.exe"));

        Assert.False(result.Success);
        Assert.Contains("PID", result.Message);
    }

    [Fact]
    public void UnsupportedAction_ShouldFailExplicitly()
    {
        var actions = new NativeMemoryActions();

        var result = actions.Execute(new CleanupAction(
            CleanupActionType.CombineMemoryPages,
            ProcessId: null,
            ProcessName: "System"));

        Assert.False(result.Success);
        Assert.Contains("暂未接入", result.Message);
    }

    [Fact]
    public void ClearSystemFileCache_ShouldFailExplicitlyUntilStableImplementationExists()
    {
        var actions = new NativeMemoryActions();

        var result = actions.Execute(new CleanupAction(
            CleanupActionType.ClearSystemFileCache,
            ProcessId: null,
            ProcessName: "System"));

        Assert.Equal(CleanupActionType.ClearSystemFileCache, result.Type);
        Assert.False(result.Success);
    }

    [Fact]
    public void SuspendAction_ShouldUseProcessExecutionPath()
    {
        var actions = new NativeMemoryActions();

        var result = actions.Execute(new CleanupAction(
            CleanupActionType.SuspendProcess,
            ProcessId: int.MaxValue,
            ProcessName: "missing.exe"));

        Assert.False(result.Success);
        Assert.Contains("打开目标进程失败", result.Message);
    }

    [Fact]
    public void ResumeAction_ShouldUseProcessExecutionPath()
    {
        var actions = new NativeMemoryActions();

        var result = actions.Execute(new CleanupAction(
            CleanupActionType.ResumeProcess,
            ProcessId: int.MaxValue,
            ProcessName: "missing.exe"));

        Assert.False(result.Success);
        Assert.Contains("打开目标进程失败", result.Message);
    }
}
