using System.Text.Json;

namespace AgentKit.Capabilities.Mcp;

/// <summary>MCP 服务注册信息。</summary>
public sealed class McpServerRegistration
{
    /// <summary>MCP 服务注册键。</summary>
    public required string Key { get; init; }

    /// <summary>服务名称。</summary>
    public required string Name { get; init; }

    /// <summary>服务描述。</summary>
    public string? Description { get; init; }

    /// <summary>传输类型。</summary>
    public required McpTransportType TransportType { get; init; }

    /// <summary>连接定义。</summary>
    public McpConnectionDefinition Connection { get; init; } = new();

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
