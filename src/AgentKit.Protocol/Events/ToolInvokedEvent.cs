using System.Text.Json;

using AgentKit.Protocol.Enums;

namespace AgentKit.Protocol.Events;

/// <summary>工具调用事件。</summary>
public sealed record ToolInvokedEvent : KitEvent
{
    /// <summary>调用 ID。</summary>
    public required string CallId { get; init; }

    /// <summary>工具名称。</summary>
    public required string ToolName { get; init; }

    /// <summary>工具类型。</summary>
    public required ToolKind ToolKind { get; init; }

    /// <summary>提供者标识。</summary>
    public required string ProviderKey { get; init; }

    /// <summary>调用参数。</summary>
    public JsonDocument? Arguments { get; init; }

    /// <summary>调用结果。</summary>
    public JsonDocument? Result { get; init; }

    /// <summary>是否调用成功。</summary>
    public bool Succeeded { get; init; }

    /// <summary>是否需要审批。</summary>
    public bool RequiresApproval { get; init; }
}
