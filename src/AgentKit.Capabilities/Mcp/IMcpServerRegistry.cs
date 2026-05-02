namespace AgentKit.Capabilities.Mcp;

/// <summary>MCP 服务注册表，通过 Key 解析 MCP 服务注册信息。</summary>
public interface IMcpServerRegistry
{
    /// <summary>尝试解析 MCP 服务注册信息。</summary>
    /// <param name="key">MCP 服务唯一键。</param>
    /// <param name="registration">MCP 服务注册信息，未找到时为 null。</param>
    /// <returns>是否找到。</returns>
    bool TryResolve(string key, out McpServerRegistration? registration);
}
