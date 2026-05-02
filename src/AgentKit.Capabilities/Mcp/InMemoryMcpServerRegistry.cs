namespace AgentKit.Capabilities.Mcp;

/// <summary>内存 MCP 服务注册表实现。</summary>
public sealed class InMemoryMcpServerRegistry : IMcpServerRegistry
{
    private readonly Dictionary<string, McpServerRegistration> _registrations = new(StringComparer.Ordinal);

    /// <summary>注册 MCP 服务。</summary>
    /// <param name="registration">MCP 服务注册信息。</param>
    public void Register(McpServerRegistration registration) => _registrations[registration.Key] = registration;

    /// <inheritdoc />
    public bool TryResolve(string key, out McpServerRegistration? registration) => _registrations.TryGetValue(key, out registration);
}
