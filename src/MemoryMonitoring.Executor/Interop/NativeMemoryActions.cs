using System.ComponentModel;
using System.Runtime.InteropServices;
using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;

namespace MemoryMonitoring.Executor.Interop;

/// <summary>
/// 封装执行器侧的 Windows 内存清理动作。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public sealed class NativeMemoryActions : INativeMemoryActions
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessSetQuota = 0x0100;
    private const uint ProcessSetInformation = 0x0200;
    private const int MemoryPriorityVeryLow = 1;
    private const uint PowerThrottlingExecutionSpeed = 0x1;

    public ActionResult Execute(CleanupAction action)
    {
        try
        {
            return action.Type switch
            {
                CleanupActionType.TrimWorkingSet => ExecuteProcessAction(action, TrimWorkingSet),
                CleanupActionType.SetMemoryPriority => ExecuteProcessAction(action, SetLowMemoryPriority),
                CleanupActionType.SetPowerThrottling => ExecuteProcessAction(action, SetPowerThrottling),
                CleanupActionType.PurgeLowPriorityStandby => ExecuteSystemInformation(action, SystemMemoryListCommand.MemoryPurgeLowPriorityStandbyList),
                CleanupActionType.PurgeStandby => ExecuteSystemInformation(action, SystemMemoryListCommand.MemoryPurgeStandbyList),
                CleanupActionType.FlushModifiedPages => ExecuteSystemInformation(action, SystemMemoryListCommand.MemoryFlushModifiedList),
                CleanupActionType.ClearSystemFileCache => Failure(action, "清理系统文件缓存暂未接入稳定实现"),
                CleanupActionType.CombineMemoryPages => Failure(action, "内存页合并暂未接入稳定实现"),
                _ => Failure(action, $"动作 {action.Type} 暂不支持真实执行")
            };
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return Failure(action, exception.Message);
        }
    }

    private static ActionResult ExecuteProcessAction(
        CleanupAction action,
        Func<SafeProcessHandle, string> execute)
    {
        if (action.ProcessId is null)
        {
            return Failure(action, "进程级动作缺少 PID");
        }

        using var process = OpenProcess(
            ProcessQueryInformation | ProcessSetQuota | ProcessSetInformation,
            inheritHandle: false,
            (uint)action.ProcessId.Value);

        if (process.IsInvalid)
        {
            return Failure(action, GetLastWin32ErrorMessage("打开目标进程失败"));
        }

        var message = execute(process);
        return Success(action, message);
    }

    private static string TrimWorkingSet(SafeProcessHandle process)
    {
        if (!EmptyWorkingSet(process))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "裁剪工作集失败");
        }

        return "已裁剪目标进程工作集";
    }

    private static string SetLowMemoryPriority(SafeProcessHandle process)
    {
        var priority = new MemoryPriorityInformation
        {
            MemoryPriority = MemoryPriorityVeryLow
        };

        if (!SetProcessInformation(
                process,
                ProcessInformationClass.ProcessMemoryPriority,
                ref priority,
                (uint)Marshal.SizeOf<MemoryPriorityInformation>()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "设置低内存优先级失败");
        }

        return "已设置低内存优先级";
    }

    private static string SetPowerThrottling(SafeProcessHandle process)
    {
        var throttling = new ProcessPowerThrottlingState
        {
            Version = 1,
            ControlMask = PowerThrottlingExecutionSpeed,
            StateMask = PowerThrottlingExecutionSpeed
        };

        if (!SetProcessInformation(
                process,
                ProcessInformationClass.ProcessPowerThrottling,
                ref throttling,
                (uint)Marshal.SizeOf<ProcessPowerThrottlingState>()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "设置后台节流失败");
        }

        return "已设置后台节流";
    }

    private static ActionResult ExecuteSystemInformation(
        CleanupAction action,
        SystemMemoryListCommand command)
    {
        var commandValue = (int)command;
        var status = NtSetSystemInformation(
            SystemInformationClass.SystemMemoryListInformation,
            ref commandValue,
            Marshal.SizeOf<int>());

        if (status != 0)
        {
            return Failure(action, $"系统级回收失败，NTSTATUS=0x{status:X8}");
        }

        return Success(action, $"已执行系统级回收：{command}");
    }

    private static ActionResult Success(CleanupAction action, string message) =>
        new(action.Type, action.ProcessId, true, message);

    private static ActionResult Failure(CleanupAction action, string message) =>
        new(action.Type, action.ProcessId, false, message);

    private static string GetLastWin32ErrorMessage(string prefix)
    {
        var error = Marshal.GetLastWin32Error();
        return $"{prefix}：{new Win32Exception(error).Message}";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(
        uint desiredAccess,
        bool inheritHandle,
        uint processId);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(SafeProcessHandle processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessInformation(
        SafeProcessHandle process,
        ProcessInformationClass processInformationClass,
        ref MemoryPriorityInformation processInformation,
        uint processInformationSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessInformation(
        SafeProcessHandle process,
        ProcessInformationClass processInformationClass,
        ref ProcessPowerThrottlingState processInformation,
        uint processInformationSize);

    [DllImport("ntdll.dll")]
    private static extern int NtSetSystemInformation(
        SystemInformationClass systemInformationClass,
        ref int systemInformation,
        int systemInformationLength);

    private enum ProcessInformationClass
    {
        ProcessMemoryPriority = 0,
        ProcessPowerThrottling = 4
    }

    private enum SystemInformationClass
    {
        SystemMemoryListInformation = 80
    }

    private enum SystemMemoryListCommand
    {
        MemoryFlushModifiedList = 3,
        MemoryPurgeStandbyList = 4,
        MemoryPurgeLowPriorityStandbyList = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryPriorityInformation
    {
        public int MemoryPriority;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessPowerThrottlingState
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    private sealed class SafeProcessHandle : SafeHandle
    {
        public SafeProcessHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero || handle == new IntPtr(-1);

        protected override bool ReleaseHandle() => CloseHandle(handle);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
