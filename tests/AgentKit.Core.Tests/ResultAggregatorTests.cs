using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Results;
using AgentKit.Protocol.Sessions;

using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>结果聚合器测试。</summary>
public class ResultAggregatorTests
{
    [Fact]
    public void Aggregate_NoSpecialEvents_ReturnsCompleted()
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

        var result = ResultAggregator.Aggregate(events, "Hello", null);

        Assert.Equal(RunState.Completed, result.RunState);
        Assert.Equal("Hello", result.FinalText);
    }

    [Fact]
    public void Aggregate_WithToolCalls_ReturnsCompletedWithToolCalls()
    {
        var events = new KitEvent[]
        {
            new ToolInvokedEvent
            {
                EventType = nameof(ToolInvokedEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                CallId = "c1", ToolName = "search", ToolKind = ToolKind.Function, ProviderKey = "maf",
            },
        };

        var result = ResultAggregator.Aggregate(events, null, null);

        Assert.Equal(RunState.CompletedWithToolCalls, result.RunState);
    }

    [Fact]
    public void Aggregate_WithApproval_ReturnsCompletedWithApproval()
    {
        var events = new KitEvent[]
        {
            new ApprovalRequiredEvent
            {
                EventType = nameof(ApprovalRequiredEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Approval = new PendingApproval
                {
                    RequestId = "req-1", CallId = "c1", TargetName = "del",
                    TargetKind = ApprovalTargetKind.FunctionTool,
                    Arguments = JsonDocument.Parse("{}"),
                },
            },
        };

        var result = ResultAggregator.Aggregate(events, null, null);

        Assert.Equal(RunState.CompletedWithApproval, result.RunState);
        Assert.NotNull(result.PendingApproval);
        Assert.Equal("req-1", result.PendingApproval.RequestId);
    }

    [Fact]
    public void Aggregate_WithFailure_ReturnsFailed()
    {
        var events = new KitEvent[]
        {
            new RunFailedEvent
            {
                EventType = nameof(RunFailedEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Failure = new Failure { Kind = FailureKind.ModelCallFailed, Message = "Error" },
            },
        };

        var result = ResultAggregator.Aggregate(events, null, null);

        Assert.Equal(RunState.Failed, result.RunState);
        Assert.NotNull(result.Failure);
        Assert.Equal(FailureKind.ModelCallFailed, result.Failure.Kind);
    }

    [Fact]
    public void Aggregate_WithStructuredValidationFailure_ReturnsFailed()
    {
        var events = new KitEvent[]
        {
            new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow, TextDelta = "bad json",
            },
        };

        var validation = new StructuredValidationResult
        {
            IsValid = false,
            FailureKind = StructuredFailureKind.InvalidJson,
            Errors = ["JSON parse error"],
            RawOutput = "bad json",
        };

        var result = ResultAggregator.Aggregate(events, "bad json", null, validation, null, null, 1);

        Assert.Equal(RunState.Failed, result.RunState);
        Assert.NotNull(result.Failure);
        Assert.Equal(FailureKind.StructuredOutputValidationFailed, result.Failure.Kind);
    }

    [Fact]
    public void Aggregate_WithStructuredValidationSuccess_ReturnsCompleted()
    {
        var events = new KitEvent[]
        {
            new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow, TextDelta = "{\"name\": \"test\"}",
            },
        };

        var validation = new StructuredValidationResult
        {
            IsValid = true,
            RawOutput = "{\"name\": \"test\"}",
        };

        var result = ResultAggregator.Aggregate(events, "{\"name\": \"test\"}", null, validation, null, null, 1);

        Assert.Equal(RunState.Completed, result.RunState);
        Assert.NotNull(result.StructuredPayload);
        Assert.Equal("test", result.StructuredPayload.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void Aggregate_TracksAttemptsUsed()
    {
        var events = Array.Empty<KitEvent>();

        var result = ResultAggregator.Aggregate(events, null, null, null, null, null, 3);

        Assert.Equal(3, result.AttemptsUsed);
    }

    [Fact]
    public void Aggregate_WithPipelineResult_Overload_Works()
    {
        var events = new KitEvent[]
        {
            new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = "r1", SessionId = "s1", Sequence = 0, Attempt = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow, TextDelta = "Hi",
            },
        };

        var pipelineResult = new PipelineResult("Hi", null, events, null, null, null, 1);
        var result = ResultAggregator.Aggregate(pipelineResult);

        Assert.Equal(RunState.Completed, result.RunState);
        Assert.Equal("Hi", result.FinalText);
    }
}
