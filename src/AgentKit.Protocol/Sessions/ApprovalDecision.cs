using System.Text.Json;

namespace AgentKit.Protocol.Sessions;

/// <summary>审批决策。</summary>
public sealed class ApprovalDecision
{
    /// <summary>审批请求 ID。</summary>
    public required string RequestId { get; init; }

    /// <summary>是否通过。</summary>
    public required bool Approved { get; init; }

    /// <summary>审批备注。</summary>
    public string? Comment { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
