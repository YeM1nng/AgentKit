using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Results;
using AgentKit.Protocol.Sessions;

using Xunit;

namespace AgentKit.Protocol.Tests;

/// <summary>失败协议契约测试。</summary>
public class FailureContractTests
{
    [Fact]
    public void Failure_Creation_Succeeds()
    {
        var failure = new Failure
        {
            Kind = FailureKind.ModelCallFailed,
            Message = "Model error",
            Detail = "Status 500",
        };

        Assert.Equal(FailureKind.ModelCallFailed, failure.Kind);
        Assert.Equal("Model error", failure.Message);
        Assert.Equal("Status 500", failure.Detail);
        Assert.Null(failure.Context);
    }

    [Fact]
    public void Failure_WithContext_Succeeds()
    {
        var context = JsonDocument.Parse("{\"statusCode\": 429}");
        var failure = new Failure
        {
            Kind = FailureKind.ContentFilter,
            Message = "Content filtered",
            Context = context,
        };

        Assert.NotNull(failure.Context);
        Assert.Equal(429, failure.Context.RootElement.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public void FailureKind_AllMembers_Defined()
    {
        var expected = new[]
        {
            FailureKind.Unknown,
            FailureKind.ModelCallFailed,
            FailureKind.ToolExecutionFailed,
            FailureKind.StructuredOutputValidationFailed,
            FailureKind.ApprovalTimeout,
            FailureKind.McpConnectionFailed,
            FailureKind.SessionRestoreFailed,
            FailureKind.DefinitionResolutionFailed,
            FailureKind.ConfigurationInvalid,
            FailureKind.ContentFilter,
            FailureKind.TokenLimitExceeded,
        };

        foreach (var kind in expected)
        {
            Assert.True(Enum.IsDefined(typeof(FailureKind), kind), $"Missing: {kind}");
        }
    }

    [Fact]
    public void RunState_AllMembers_Defined()
    {
        var expected = new[]
        {
            RunState.Running,
            RunState.Completed,
            RunState.CompletedWithToolCalls,
            RunState.CompletedWithApproval,
            RunState.CompletedWithContinuation,
            RunState.Failed,
            RunState.FailedContentFilter,
            RunState.FailedTokenLimit,
        };

        foreach (var state in expected)
        {
            Assert.True(Enum.IsDefined(typeof(RunState), state), $"Missing: {state}");
        }
    }

    [Fact]
    public void RunResult_Defaults_Succeeds()
    {
        var result = new RunResult
        {
            RunState = RunState.Completed,
        };

        Assert.Equal(RunState.Completed, result.RunState);
        Assert.Null(result.FinalText);
        Assert.Null(result.Failure);
        Assert.Null(result.SessionState);
        Assert.Equal(0, result.AttemptsUsed);
    }

    [Fact]
    public void StructuredValidationResult_Defaults_Succeeds()
    {
        var validation = new StructuredValidationResult
        {
            IsValid = false,
            FailureKind = StructuredFailureKind.InvalidJson,
            Errors = ["Unexpected token at position 5"],
            RawOutput = "{invalid}",
        };

        Assert.False(validation.IsValid);
        Assert.Equal(StructuredFailureKind.InvalidJson, validation.FailureKind);
        Assert.Single(validation.Errors);
    }

    [Fact]
    public void PendingApproval_Creation_Succeeds()
    {
        var approval = new PendingApproval
        {
            RequestId = "req-1",
            CallId = "call-1",
            TargetName = "delete-file",
            TargetKind = ApprovalTargetKind.FunctionTool,
            Arguments = JsonDocument.Parse("{\"path\": \"/tmp/file.txt\"}"),
            Reason = "Destructive operation",
        };

        Assert.Equal("req-1", approval.RequestId);
        Assert.Equal("delete-file", approval.TargetName);
        Assert.Equal(ApprovalTargetKind.FunctionTool, approval.TargetKind);
    }

    [Fact]
    public void ApprovalDecision_Creation_Succeeds()
    {
        var decision = new ApprovalDecision
        {
            RequestId = "req-1",
            Approved = true,
            Comment = "Approved by admin",
        };

        Assert.True(decision.Approved);
        Assert.Equal("Approved by admin", decision.Comment);
    }
}
