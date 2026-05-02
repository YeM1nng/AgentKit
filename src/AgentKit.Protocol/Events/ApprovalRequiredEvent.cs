using AgentKit.Protocol.Sessions;

namespace AgentKit.Protocol.Events;

/// <summary>审批请求事件。</summary>
public sealed record ApprovalRequiredEvent : KitEvent
{
    /// <summary>待审批项。</summary>
    public required PendingApproval Approval { get; init; }
}
