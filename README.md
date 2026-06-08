# Windows 内存守护器

面向 Windows 的轻量级桌面内存监控与自动回收工具。项目采用 WPF 主程序 + 独立执行器的双进程结构：主程序负责采样、规则、趋势图、托盘与自动化策略，执行器负责需要更高权限的内存治理动作。

## 当前能力

- 实时系统内存采样、Top 进程采样与趋势图展示。
- 白名单、仅清理、均衡、可挂起四类规则管理。
- 进程工作集裁剪、内存优先级调整、后台节流、挂起与恢复动作。
- Standby 列表与 Modified Page List 系统级回收动作。
- 持续高压触发自动清理，并受全局冷却时间保护。
- 启动后、唤醒后延迟软清理。
- 托盘入口：最小化隐藏、托盘恢复、托盘退出。
- 当前用户开机自启注册服务，默认关闭，可在设置页通过“随 Windows 启动”开关启用。
- JSON 配置、规则持久化与 JSONL 动作日志。

## 项目结构

- `src/MemoryMonitoring.App`：WPF 桌面应用、视图模型、趋势控件与托盘入口。
- `src/MemoryMonitoring.Core`：领域模型、策略评估、清理计划与执行器契约。
- `src/MemoryMonitoring.Infrastructure`：采样、持久化、管道通信、电源事件与自启注册。
- `src/MemoryMonitoring.Executor`：独立执行器与 Windows 原生动作封装。
- `tests/MemoryMonitoring.Tests`：xUnit 单元测试。
- `docs/superpowers/specs`：设计说明。
- `docs/superpowers/plans`：开发计划。

## 本地运行

```powershell
dotnet restore MemoryMonitoring.sln
dotnet build MemoryMonitoring.sln
dotnet run --project src\MemoryMonitoring.App\MemoryMonitoring.App.csproj
```

## 验证

```powershell
dotnet test MemoryMonitoring.sln
dotnet build MemoryMonitoring.sln
```

当前目标是保持全量测试通过、构建无警告，并在接入高风险系统动作前保留明确的保护策略与失败日志。
