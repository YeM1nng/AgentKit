namespace AgentKit.Capabilities.Mcp;

/// <summary>MCP 连接定义。</summary>
public sealed class McpConnectionDefinition
{
    /// <summary>启动命令（stdio 模式）。</summary>
    public string? Command { get; init; }

    /// <summary>命令参数（stdio 模式）。</summary>
    public IReadOnlyList<string>? Arguments { get; init; }

    /// <summary>工作目录（stdio 模式）。</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>环境变量（stdio 模式）。</summary>
    public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>服务 URL（streamable-http 模式）。</summary>
    public string? Url { get; init; }

    /// <summary>请求头（streamable-http 模式）。</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>请求头提供者键标识。</summary>
    public string? HeaderProviderKey { get; init; }

    /// <summary>连接超时秒数。</summary>
    public int TimeoutSeconds { get; init; } = 30;
}
