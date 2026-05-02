using System.Text.Json;

using AgentKit.Maf.Approvals;
using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Sessions;

using Microsoft.Extensions.AI;

using Xunit;

namespace AgentKit.Maf.Tests;

/// <summary>审批恢复工厂测试。</summary>
public class ApprovalResumeFactoryTests
{
    [Fact]
    public void CreateResumeMessage_Approved_Succeeds()
    {
        var pending = new PendingApproval
        {
            RequestId = "req-1",
            CallId = "call-1",
            TargetName = "search",
            TargetKind = ApprovalTargetKind.FunctionTool,
            Arguments = JsonDocument.Parse("{\"query\": \"test\"}"),
            Reason = "Needs review",
        };

        var decision = new ApprovalDecision
        {
            RequestId = "req-1",
            Approved = true,
            Comment = "OK to proceed",
        };

        var message = ApprovalResumeFactory.CreateResumeMessage(pending, decision);

        Assert.Equal(ChatRole.User, message.Role);
        Assert.NotNull(message.Contents);
        Assert.Equal(2, message.Contents.Count);
    }

    [Fact]
    public void CreateResumeMessage_Rejected_Succeeds()
    {
        var pending = new PendingApproval
        {
            RequestId = "req-2",
            CallId = "call-2",
            TargetName = "delete-file",
            TargetKind = ApprovalTargetKind.McpTool,
            Arguments = JsonDocument.Parse("{\"path\": \"/important\"}"),
        };

        var decision = new ApprovalDecision
        {
            RequestId = "req-2",
            Approved = false,
            Comment = "Too dangerous",
        };

        var message = ApprovalResumeFactory.CreateResumeMessage(pending, decision);

        Assert.Equal(ChatRole.User, message.Role);
        Assert.NotNull(message.Contents);
        Assert.Equal(2, message.Contents.Count);
    }

    [Fact]
    public void CreateResumeMessage_EmptyArguments_Succeeds()
    {
        var pending = new PendingApproval
        {
            RequestId = "req-3",
            CallId = "call-3",
            TargetName = "no-args-tool",
            TargetKind = ApprovalTargetKind.FunctionTool,
            Arguments = JsonDocument.Parse("{}"),
        };

        var decision = new ApprovalDecision
        {
            RequestId = "req-3",
            Approved = true,
        };

        var message = ApprovalResumeFactory.CreateResumeMessage(pending, decision);

        Assert.NotNull(message);
        Assert.Equal(2, message.Contents.Count);
    }
}
