using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Sessions;

using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>审批协调器测试。</summary>
public class ApprovalCoordinatorTests
{
    [Fact]
    public void ExtractPendingApproval_WithApprovalEvent_ReturnsApproval()
    {
        var approval = new PendingApproval
        {
            RequestId = "req-1",
            CallId = "call-1",
            TargetName = "delete",
            TargetKind = ApprovalTargetKind.FunctionTool,
            Arguments = JsonDocument.Parse("{}"),
        };

        var events = new KitEvent[]
        {
            new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow, TextDelta = "About to delete...",
            },
            new ApprovalRequiredEvent
            {
                EventType = nameof(ApprovalRequiredEvent),
                RunId = "r1", SessionId = "s1", Sequence = 1, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow, Approval = approval,
            },
        };

        var result = ApprovalCoordinator.ExtractPendingApproval(events);

        Assert.NotNull(result);
        Assert.Equal("req-1", result.RequestId);
        Assert.Equal("delete", result.TargetName);
    }

    [Fact]
    public void ExtractPendingApproval_NoApprovalEvent_ReturnsNull()
    {
        var events = new KitEvent[]
        {
            new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow, TextDelta = "Hello",
            },
        };

        var result = ApprovalCoordinator.ExtractPendingApproval(events);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractPendingApproval_EmptyEvents_ReturnsNull()
    {
        var result = ApprovalCoordinator.ExtractPendingApproval([]);
        Assert.Null(result);
    }
}
