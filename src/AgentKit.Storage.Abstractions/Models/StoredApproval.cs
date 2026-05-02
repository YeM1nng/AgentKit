using System.Text.Json;

namespace AgentKit.Storage.Models;

/// <summary>持久化存储的审批单。</summary>
public sealed class StoredApproval
{
    /// <summary>审批请求 ID。</summary>
    public required string RequestId { get; init; }

    /// <summary>运行 ID。</summary>
    public required string RunId { get; init; }

    /// <summary>会话 ID。</summary>
    public required string SessionId { get; init; }

    /// <summary>审批目标类型。</summary>
    public required ApprovalTargetKind TargetKind { get; init; }

    /// <summary>审批目标名称。</summary>
    public required string TargetName { get; init; }

    /// <summary>调用参数。</summary>
    public required JsonDocument Arguments { get; init; }

    /// <summary>审批原因。</summary>
    public string? Reason { get; init; }

    /// <summary>助手文本。</summary>
    public string? AssistantText { get; init; }

    /// <summary>审批状态。</summary>
    public string Status { get; init; } = "pending";

    /// <summary>过期时间（UTC）。</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
