using AgentKit.Protocol.Events;
using AgentKit.Protocol.Sessions;

namespace AgentKit.Core;

/// <summary>从事件中提取待审批项。</summary>
public static class ApprovalCoordinator
{
    /// <summary>从事件列表中提取待审批项。</summary>
    /// <param name="events">事件列表。</param>
    /// <returns>待审批项，无则返回 null。</returns>
    public static PendingApproval? ExtractPendingApproval(IReadOnlyList<KitEvent> events)
    {
        for (var i = events.Count - 1; i >= 0; i--)
        {
            if (events[i] is ApprovalRequiredEvent approvalEvent)
                return approvalEvent.Approval;
        }
        return null;
    }
}
