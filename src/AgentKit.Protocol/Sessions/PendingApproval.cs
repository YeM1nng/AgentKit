using System.Text.Json;

using AgentKit.Protocol.Enums;

namespace AgentKit.Protocol.Sessions;

/// <summary>待审批项。</summary>
public sealed class PendingApproval
{
    /// <summary>审批请求 ID。</summary>
    public required string RequestId { get; init; }

    /// <summary>工具调用 ID。</summary>
    public required string CallId { get; init; }

    /// <summary>目标名称。</summary>
    public required string TargetName { get; init; }

    /// <summary>目标类型。</summary>
    public required ApprovalTargetKind TargetKind { get; init; }

    /// <summary>调用参数。</summary>
    public required JsonDocument Arguments { get; init; }

    /// <summary>审批原因。</summary>
    public string? Reason { get; init; }

    /// <summary>助手文本。</summary>
    public string? AssistantText { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
