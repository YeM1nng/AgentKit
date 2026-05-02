using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;

using Xunit;

namespace AgentKit.Protocol.Tests;

/// <summary>事件协议契约测试。</summary>
public class EventContractTests
{
    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    [Fact]
    public void ResponseDeltaEvent_Creation_Succeeds()
    {
        var ev = new ResponseDeltaEvent
        {
            EventType = nameof(ResponseDeltaEvent),
            RunId = "run-1",
            SessionId = "sess-1",
            Sequence = 0,
            Attempt = 1,
            OccurredAtUtc = Now,
            TextDelta = "Hello",
        };

        Assert.Equal("ResponseDeltaEvent", ev.EventType);
        Assert.Equal("Hello", ev.TextDelta);
    }

    [Fact]
    public void ToolInvokedEvent_Creation_Succeeds()
    {
        var ev = new ToolInvokedEvent
        {
            EventType = nameof(ToolInvokedEvent),
            RunId = "run-1",
            SessionId = "sess-1",
            Sequence = 1,
            Attempt = 1,
            OccurredAtUtc = Now,
            CallId = "call-123",
            ToolName = "search",
            ToolKind = ToolKind.Function,
            ProviderKey = "maf",
            Arguments = JsonDocument.Parse("{\"query\": \"test\"}"),
            Succeeded = true,
        };

        Assert.Equal("call-123", ev.CallId);
        Assert.Equal("search", ev.ToolName);
        Assert.Equal(ToolKind.Function, ev.ToolKind);
        Assert.True(ev.Succeeded);
        Assert.False(ev.RequiresApproval);
    }

    [Fact]
    public void RunFailedEvent_Creation_Succeeds()
    {
        var failure = new Results.Failure
        {
            Kind = FailureKind.ModelCallFailed,
            Message = "Model returned error",
        };

        var ev = new RunFailedEvent
        {
            EventType = nameof(RunFailedEvent),
            RunId = "run-1",
            SessionId = "sess-1",
            Sequence = 2,
            Attempt = 1,
            OccurredAtUtc = Now,
            Failure = failure,
        };

        Assert.Equal(FailureKind.ModelCallFailed, ev.Failure.Kind);
        Assert.Equal("Model returned error", ev.Failure.Message);
    }

    [Fact]
    public void KitEvent_RecordEquality_Succeeds()
    {
        var time = Now;
        var ev1 = new ResponseDeltaEvent
        {
            EventType = "ResponseDeltaEvent",
            RunId = "run-1",
            SessionId = "sess-1",
            Sequence = 0,
            Attempt = 1,
            OccurredAtUtc = time,
            TextDelta = "Hi",
        };

        var ev2 = ev1 with { };

        Assert.Equal(ev1, ev2);
    }

    [Fact]
    public void KitEvent_WithExpression_Succeeds()
    {
        var ev = new ToolInvokedEvent
        {
            EventType = nameof(ToolInvokedEvent),
            RunId = "run-1",
            SessionId = "sess-1",
            Sequence = 0,
            Attempt = 1,
            OccurredAtUtc = Now,
            CallId = "call-1",
            ToolName = "search",
            ToolKind = ToolKind.Function,
            ProviderKey = "maf",
        };

        var updated = ev with { Sequence = 5, Succeeded = true };

        Assert.Equal(5, updated.Sequence);
        Assert.True(updated.Succeeded);
        Assert.Equal("call-1", updated.CallId);
    }
}
