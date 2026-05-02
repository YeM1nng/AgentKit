namespace AgentKit.Capabilities.Tools;

/// <summary>工具注册表，通过 Key 解析工具注册信息。</summary>
public interface IToolRegistry
{
    /// <summary>尝试解析工具注册信息。</summary>
    /// <param name="key">工具唯一键。</param>
    /// <param name="registration">工具注册信息，未找到时为 null。</param>
    /// <returns>是否找到。</returns>
    bool TryResolve(string key, out ToolRegistration? registration);
}
