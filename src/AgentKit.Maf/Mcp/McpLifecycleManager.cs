using System.Collections.Concurrent;

using AgentKit.Capabilities.Mcp;

using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Mcp;

/// <summary>管理 MCP Server 工具列表的缓存生命周期。</summary>
public sealed class McpLifecycleManager : IDisposable
{
    private readonly McpToolsetFactory _toolsetFactory;
    private readonly ConcurrentDictionary<string, IReadOnlyList<AITool>> _toolCache = new(StringComparer.Ordinal);
    private volatile bool _disposed;

    /// <summary>构造函数。</summary>
    /// <param name="toolsetFactory">MCP 工具集工厂。</param>
    public McpLifecycleManager(McpToolsetFactory toolsetFactory)
    {
        _toolsetFactory = toolsetFactory;
    }

    /// <summary>获取 MCP Server 的工具列表（带缓存）。</summary>
    /// <param name="serverKey">MCP 服务注册键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>工具列表。</returns>
    public async Task<IReadOnlyList<AITool>> GetToolsAsync(string serverKey, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_toolCache.TryGetValue(serverKey, out var cached))
            return cached;

        var tools = await _toolsetFactory.GetToolsAsync(serverKey, cancellationToken);
        _toolCache[serverKey] = tools;
        return tools;
    }

    /// <summary>清除指定 Server 的工具缓存。</summary>
    /// <param name="serverKey">MCP 服务注册键。</param>
    /// <returns>是否成功清除。</returns>
    public bool InvalidateCache(string serverKey) => _toolCache.TryRemove(serverKey, out _);

    /// <summary>清除全部工具缓存。</summary>
    public void ClearCache() => _toolCache.Clear();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _toolCache.Clear();
    }
}
