namespace MemoryMonitoring.Infrastructure.Startup;

/// <summary>
/// 抽象当前用户启动项注册表写入，便于测试与隔离系统依赖。
/// 作者：OpenAI Codex
/// 版本：1.0
/// </summary>
public interface IStartupRegistry
{
    void SetValue(string name, string value);

    void DeleteValue(string name);
}
