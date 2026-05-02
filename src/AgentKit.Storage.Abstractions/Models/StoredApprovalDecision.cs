using System.Text.Json;

namespace AgentKit.Storage.Models;

/// <summary>持久化存储的审批决策。</summary>
public sealed class StoredApprovalDecision
{
    /// <summary>审批请求 ID。</summary>
    public required string RequestId { get; init; }

    /// <summary>是否通过。</summary>
    public required bool Approved { get; init; }

    /// <summary>审批意见。</summary>
    public string? Comment { get; init; }

    /// <summary>审批人。</summary>
    public string? DecidedBy { get; init; }

    /// <summary>决策时间（UTC）。</summary>
    public DateTimeOffset DecidedAtUtc { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
