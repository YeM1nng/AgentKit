using AgentKit.Capabilities.Mcp;

using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Mcp;

/// <summary>MCP 工具集工厂，通过注册表解析 MCP 工具。</summary>
/// <remarks>
/// 实际的 MCP 连接与工具发现由外部实现提供。
/// 此工厂负责将 MCP 注册信息映射为可用的 AITool 列表。
/// </remarks>
public sealed class McpToolsetFactory
{
    private readonly IMcpServerRegistry _registry;
    private readonly Func<McpServerRegistration, CancellationToken, Task<IReadOnlyList<AITool>>> _toolDiscovery;

    /// <summary>构造函数。</summary>
    /// <param name="registry">MCP 服务注册表。</param>
    /// <param name="toolDiscovery">工具发现委托，接收注册信息返回工具列表。</param>
    public McpToolsetFactory(
        IMcpServerRegistry registry,
        Func<McpServerRegistration, CancellationToken, Task<IReadOnlyList<AITool>>> toolDiscovery)
    {
        _registry = registry;
        _toolDiscovery = toolDiscovery;
    }

    /// <summary>获取指定 MCP Server 的工具列表。</summary>
    /// <param name="serverKey">MCP 服务注册键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>工具列表。</returns>
    public async Task<IReadOnlyList<AITool>> GetToolsAsync(string serverKey, CancellationToken cancellationToken)
    {
        if (!_registry.TryResolve(serverKey, out var registration) || registration is null)
            throw new InvalidOperationException($"MCP Server '{serverKey}' 未在注册表中找到。");

        return await _toolDiscovery(registration, cancellationToken);
    }
}
