# 系统级内存动作稳定性评估

## 结论

`ClearSystemFileCache` 与 `CombineMemoryPages` 暂不接入真实执行，继续保持显式失败。

原因是当前没有找到足够稳定、官方支持且可维护的用户态实现路径。它们通常依赖 `ntdll.dll` 内部接口、未完整公开的 `SYSTEM_INFORMATION_CLASS` / 命令值或版本相关行为。直接接入会带来系统卡顿、不可预测失败、Windows 版本兼容性和误导用户的问题。

## 评估依据

- Microsoft Learn 的 `NtQuerySystemInformation` 文档说明该函数及其关联结构属于系统内部实现，并可能随 Windows  版本变化；如果使用，应动态链接并准备优雅降级。参考：https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation
- Microsoft Learn 的 `winternl.h` 页面仅列出部分内部 API，未提供 `NtSetSystemInformation` 对 `SystemMemoryListInformation`、文件缓存清理或内存页合并的稳定用户态契约。参考：https://learn.microsoft.com/en-us/windows/win32/api/winternl/
- 当前执行器已接入的系统动作只保留在已有计划中明确允许的稳定子集：`PurgeLowPriorityStandby`、`PurgeStandby`、`FlushModifiedPages`。`ClearSystemFileCache` 与 `CombineMemoryPages` 没有同等验证依据。

## 当前策略

- `ClearSystemFileCache`：返回失败，消息说明“暂未接入稳定实现”。
- `CombineMemoryPages`：返回失败，消息说明“暂未接入稳定实现”。
- `CleanupPlanBuilder` 不会在默认或高压清理计划中生成这两个动作。
- 单元测试锁定上述行为，防止未来误把未验证动作加入默认执行路径。

## 后续解锁条件

只有同时满足以下条件，才允许重新评估接入：

- 找到官方支持或可充分验证的 API/命令契约。
- 在至少 Windows 10 22H2、Windows 11 23H2/24H2 上完成手工验证。
- 明确权限要求、失败码和回退路径。
- 默认保持关闭，并在 UI 中标注为高级/风险动作。
- 失败不能阻塞后续低风险动作。
