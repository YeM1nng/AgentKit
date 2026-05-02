using System.Text.Json;

namespace AgentKit.Protocol.Definitions;

/// <summary>MCP 服务引用。</summary>
public sealed class McpServerReference
{
    /// <summary>MCP 服务注册键。</summary>
    public required string Key { get; init; }

    /// <summary>是否启用。</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>是否需要审批。</summary>
    public bool RequiresApproval { get; init; }

    /// <summary>审批原因说明。</summary>
    public string? ApprovalReason { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
