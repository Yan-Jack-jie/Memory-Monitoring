# Windows 内存守护器设计说明

**日期：** 2026-06-03  
**状态：** 已确认方案 2，进入计划阶段  
**目标平台：** Windows 10 1809+ / Windows 11  
**方案结论：** `UI + 提权执行器` 双进程架构

---

## 1. 项目目标

构建一个面向 Windows 的轻量桌面内存工具，提供以下能力：

- 实时显示系统内存占用和近期趋势
- 监控高占用、低活跃进程
- 在内存持续告急时自动执行分级回收
- 支持白名单，白名单应用绝不自动挂起
- 支持开机后、唤醒后、长时间运行后的自动软回收
- 对浏览器、多开常驻工具、低活跃后台应用提供差异化策略

本项目不是“点一下就神奇释放所有内存”的工具，而是“策略驱动的混合型内存调度器”。

---

## 2. 已确认的产品边界

### 2.1 已确认决策

- 当前只做 Windows 平台
- 接受管理员权限运行
- 接受部分 undocumented 系统接口
- 自动处理强度采用第 2 档“平衡”
- 提供白名单，白名单进程永不自动挂起
- 主窗口中需要展示内存占用
- 允许展示近期内存变化，但前提是采样成本足够低

### 2.2 非目标

以下内容不进入第一阶段：

- 跨平台支持
- 驱动级内核开发
- 自动结束用户进程
- 对第三方应用“真正释放内部缓存”的承诺
- 浏览器扩展级标签管理
- 分布式监控、远程控制、多机协同

---

## 3. 核心工程判断

### 3.1 不能把“清理缓存”当作统一能力

对任意第三方进程，应用本身通常没有“公开清理内部缓存”的接口。第一阶段能稳定执行的动作主要是：

- 裁剪进程 working set
- 调低内存优先级
- 对后台进程启用 power throttling
- 执行系统级内存列表回收
- 对明确允许的进程执行挂起

因此产品文案和实现都必须避免伪承诺。对于第三方进程，第一阶段做的是“让 Windows 更积极地回收这些进程占用的物理页”，不是“命令目标应用释放它的内部对象图”。

### 3.2 “压缩休眠进程”在实现上要转译

Windows 的 memory compression 是系统级机制，不是稳定的单进程公开 API。对本工具来说，“压缩休眠进程”应转译为：

- 将低活跃进程标记为低内存优先级
- 对其执行 working set trim
- 必要时结合后台节流
- 仅在用户允许时对其挂起

### 3.3 近期趋势图可做且开销可控

近期趋势图不需要 ETW 全量追踪。采用轻量环形采样即可：

- 系统内存：每 3 秒采样一次
- 进程 Top N：每 10 秒采样一次
- 24 小时趋势只保存聚合点，不保存全量明细

该设计的内存和 CPU 开销都可控，符合“轻量化优先”的要求。

---

## 4. 推荐技术方案

### 4.1 技术栈

- UI：`.NET 8 + WPF`
- 执行器：`.NET 8 Console/Worker + P/Invoke`
- 通信：`Named Pipes + System.Text.Json`
- 采样与配置：`.NET BCL`
- 测试：`xUnit`

### 4.2 为什么不先选 WinUI 3

WinUI 3 是 Microsoft 当前主推的现代 Windows UI 框架，运行在 Windows App SDK 上。  
但基于当前项目目标，我更推荐第一版使用 WPF，原因如下：

- 本项目是系统托盘工具，不是内容型应用
- 需要高密度数据表格、托盘常驻、后台交互、管理员辅助进程
- WPF 在桌面工具、托盘程序、原生 Win32 互操作方面更成熟
- 第一版目标是把监控与策略跑通，不是优先追求现代视觉框架

这是基于当前需求做出的工程取舍，不代表 WinUI 3 不可用。

---

## 5. 总体架构

采用双进程结构：

1. `MemoryMonitoring.App`
   - 普通桌面 UI 进程
   - 负责主窗口、托盘、趋势图、规则配置、日志展示
   - 负责监控采样、策略评估、触发调度

2. `MemoryMonitoring.Executor`
   - 提权执行器
   - 负责真正执行高权限操作
   - 负责所有危险动作与 undocumented API 调用

3. `MemoryMonitoring.Core`
   - 共享领域模型、策略规则、命令契约

4. `MemoryMonitoring.Infrastructure`
   - 采样、配置存储、命名管道、系统事件桥接、日志落盘

### 5.1 为什么 UI 进程不直接提权

不建议把整个 UI 进程都做成管理员权限：

- 日常使用风险更高
- UI 与高权限操作耦合，后续维护差
- 托盘、开机自启、进程重启时用户体验更差

因此采用普通 UI + 按需提权执行器是更稳妥的第一版设计。

---

## 6. 建议目录结构

```text
Memory Monitoring/
├─ src/
│  ├─ MemoryMonitoring.App/
│  ├─ MemoryMonitoring.Core/
│  ├─ MemoryMonitoring.Infrastructure/
│  └─ MemoryMonitoring.Executor/
├─ tests/
│  └─ MemoryMonitoring.Tests/
├─ docs/
│  └─ superpowers/
│     ├─ specs/
│     └─ plans/
└─ README.md
```

---

## 7. 模块设计

## 7.1 Monitor 模块

职责：

- 采集系统物理内存、可用内存、提交量、缓存量
- 采集进程 working set、private bytes、句柄数、线程数
- 识别候选目标进程
- 维护近期趋势采样数据

建议采样项：

- 系统级
  - `MemoryLoad`
  - `AvailablePhysical`
  - `TotalPhysical`
  - `AvailablePageFile`
  - `CommitUsed`
  - `SystemCache`
- 进程级
  - `ProcessId`
  - `ProcessName`
  - `WorkingSetBytes`
  - `PrivateBytes`
  - `PagedMemoryBytes`
  - `HandleCount`
  - `StartTime`
  - `LastForegroundSeenAt`
  - `RuleCategory`

### 7.2 Policy Engine 模块

职责：

- 根据系统压力、进程活跃度、分类规则生成清理计划
- 执行冷却时间和保护规则
- 决定采取软动作、中动作还是强动作

规则输入：

- 当前系统内存快照
- 最近一段时间的历史曲线
- 进程分类规则
- 白名单和挂起许可
- 开机后/唤醒后状态
- 最近一次动作日志

策略输出：

- `NoAction`
- `SoftCleanup`
- `TargetedCleanup`
- `SystemCleanup`
- `TargetedSuspend`

### 7.3 Executor 模块

职责：

- 接收 UI 进程生成的清理计划
- 逐项执行高权限动作
- 返回执行结果、耗时、成功与失败原因

动作清单：

- `TrimProcessWorkingSet`
- `SetProcessMemoryPriority`
- `SetProcessPowerThrottling`
- `PurgeLowPriorityStandbyList`
- `PurgeStandbyList`
- `FlushModifiedPageList`
- `ClearSystemFileCache`
- `CombineMemoryPages`
- `SuspendProcess`
- `ResumeProcess`

### 7.4 History Store 模块

职责：

- 保存近期系统内存趋势
- 保存 Top N 进程的阶段性快照
- 保存自动动作日志

存储原则：

- 系统趋势使用内存环形缓冲，主打轻量
- 配置与日志落盘到本地 JSON / JSONL
- 不引入数据库

---

## 8. 进程分类模型

第一版建议支持四类规则：

1. `Whitelist`
   - 永不自动挂起
   - 默认不进入激进行为
   - 允许温和 trim，但需可单独关闭

2. `TrimOnly`
   - 允许自动 trim
   - 允许自动降内存优先级
   - 不允许自动挂起

3. `Balanced`
   - 默认后台应用类别
   - 允许 trim、低优先级、节流
   - 仅在显式授权时可升级到挂起

4. `SuspendEligible`
   - 用户手动标记
   - 允许在高压状态下自动挂起

### 8.1 默认分类建议

- 微信：`TrimOnly`
- VPN：`Whitelist`
- Cherry Studio：`TrimOnly`
- 浏览器：`TrimOnly`
- 下载器、更新器、同步器：`Balanced`

说明：

- 浏览器第一版默认只做 trim 和低优先级，不自动挂起整个浏览器进程
- VPN 永不挂起，避免网络中断

---

## 9. 自动化策略分层

第一版采用分层策略，不做单点阈值触发。

### 9.1 触发器

1. 持续高压触发
   - 可用内存低于阈值，且持续 N 秒
   - 或内存负载高于阈值，且持续 N 秒

2. 启动后触发
   - 应用开机自启后延迟 60-120 秒执行一轮软回收

3. 唤醒后触发
   - 从睡眠/休眠恢复后延迟 30-90 秒执行一轮软回收

4. 周期性软回收
   - 每隔数小时执行一次仅针对后台低活跃进程的温和回收

### 9.2 动作阶梯

#### Level 1：软动作

- 对低活跃后台进程做 working set trim
- 设置低内存优先级
- 启用后台 power throttling

#### Level 2：中动作

- 对候选进程批量 trim
- 系统级低风险回收：
  - `PurgeLowPriorityStandbyList`
  - `CombineMemoryPages`

#### Level 3：强动作

- 在高压持续存在时执行更强系统回收：
  - `PurgeStandbyList`
  - `FlushModifiedPageList`
  - `ClearSystemFileCache`

#### Level 4：挂起动作

- 仅对 `SuspendEligible` 类别生效
- 白名单和 `TrimOnly` 绝不自动挂起

### 9.3 保护机制

- 单进程冷却时间
- 全局冷却时间
- 白名单绝对保护
- 前台窗口所属进程保护
- 刚启动进程保护
- 用户正在高交互时避免激进动作

---

## 10. UI 设计框架

主窗口建议分为四个区域。

### 10.1 顶部状态区

展示：

- 当前物理内存使用率
- 当前可用内存
- 当前提交量
- 当前系统缓存
- 当前自动化状态

### 10.2 趋势区

展示：

- 近 30 分钟
- 近 6 小时
- 近 24 小时

指标：

- 已用物理内存
- 可用物理内存
- 动作触发点标记

实现建议：

- 使用自绘折线或轻量控件
- 不引入重型图表库作为第一优先

### 10.3 进程区

展示 Top N 进程：

- 进程名
- PID
- Working Set
- Private Bytes
- 分类
- 最近活跃时间
- 最近一次动作

支持：

- 加入白名单
- 设为仅 trim
- 允许挂起
- 立即手动回收

### 10.4 日志与规则区

展示：

- 最近动作记录
- 执行动作、耗时、结果
- 命中原因
- 当前规则列表

---

## 11. 配置与持久化

建议本地配置文件：

```text
%LocalAppData%/MemoryMonitoring/
├─ settings.json
├─ rules.json
├─ action-log.jsonl
└─ snapshots/
```

### 11.1 `settings.json`

内容：

- 采样间隔
- 自动化开关
- 内存压力阈值
- 启动后延迟
- 唤醒后延迟
- 冷却时间

### 11.2 `rules.json`

内容：

- 白名单
- TrimOnly 列表
- SuspendEligible 列表
- 自定义阈值覆盖

### 11.3 `action-log.jsonl`

内容：

- 时间
- 触发来源
- 目标进程
- 执行动作
- 执行结果
- 回收前后关键指标

---

## 12. 安全与权限模型

### 12.1 权限原则

- UI 默认普通权限运行
- 执行器仅在需要时提权
- 所有 undocumented API 都封装在执行器内

### 12.2 失败处理

- 提权失败时，UI 需要明确提示
- 执行器调用失败时记录完整错误上下文
- 某个动作失败不能阻塞后续非危险动作

### 12.3 风险说明

以下动作需要显著标记为高级功能：

- `PurgeStandbyList`
- `FlushModifiedPageList`
- `ClearSystemFileCache`
- `SuspendProcess`

原因是这些动作更容易引起系统抖动、应用卡顿或短时 IO 峰值。

---

## 13. 可验证指标

第一版是否有效，不以“任务管理器视觉变小”作为唯一标准，而以以下指标判断：

- 高压状态下，可用内存是否回升
- 回收后 1 分钟内是否明显反弹
- 白名单应用是否未被误挂起
- 浏览器、微信、VPN 是否保持可用
- 开机后与唤醒后是否能稳定完成一轮软回收
- 工具自身 CPU/内存开销是否可控

建议目标：

- UI 常驻内存小于 150 MB
- 空闲 CPU 占用长期低于 1%
- 24 小时趋势采样额外内存占用控制在数 MB 级别

---

## 14. 第一阶段里程碑

### 阶段 1：最小可用监控器

- 系统内存采样
- 主窗口总览
- 趋势图
- 进程列表

### 阶段 2：目标进程软回收

- 规则系统
- 白名单
- Working set trim
- 内存优先级调整
- 后台节流

### 阶段 3：系统级回收

- standby list
- file cache
- memory combine
- 动作日志

### 阶段 4：自动化与挂起

- 开机后/唤醒后策略
- 冷却机制
- 用户授权挂起

---

## 15. 后续关键决策

后续进入实现前，还需要坚持以下约束：

- 第一版不为追求“数字看起来下降”而默认启用最激进动作
- 第一版先把规则、日志、保护机制做对
- 浏览器默认不自动挂起
- 白名单默认为强保护，而不是弱建议

---

## 16. 参考资料

- [WinUI 3 - Windows apps | Microsoft Learn](https://learn.microsoft.com/en-gb/windows/apps/winui/)
- [Windows App SDK | Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [CreateMemoryResourceNotification](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-creatememoryresourcenotification)
- [EmptyWorkingSet](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-emptyworkingset)
- [SetProcessWorkingSetSizeEx](https://learn.microsoft.com/zh-cn/windows/win32/api/memoryapi/nf-memoryapi-setprocessworkingsetsizeex)
- [MEMORY_PRIORITY_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-memory_priority_information)
- [PROCESS_POWER_THROTTLING_STATE](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-process_power_throttling_state)
- [QueryUnbiasedInterruptTime](https://learn.microsoft.com/en-us/windows/win32/api/realtimeapiset/nf-realtimeapiset-queryunbiasedinterrupttime)
- [NtSetSystemInformation](https://learn.microsoft.com/zh-tw/windows/win32/sysinfo/ntsetsysteminformation)
- [Mem Reduct](https://github.com/henrypp/memreduct)
