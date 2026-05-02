using AgentKit.Protocol.Requests;
using AgentKit.Protocol.Sessions;

using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>会话协调器测试。</summary>
public class SessionCoordinatorTests
{
    [Fact]
    public void ResolveSessionId_WithSession_ReturnsExistingId()
    {
        var request = new RunRequest
        {
            Input = "hello",
            Session = new SessionState { SessionId = "existing-session" },
        };

        var id = SessionCoordinator.ResolveSessionId(request);
        Assert.Equal("existing-session", id);
    }

    [Fact]
    public void ResolveSessionId_WithoutSession_GeneratesNewId()
    {
        var request = new RunRequest { Input = "hello" };

        var id = SessionCoordinator.ResolveSessionId(request);

        Assert.NotNull(id);
        Assert.NotEmpty(id);
    }

    [Fact]
    public void ResolveSessionId_GeneratesUniqueIds()
    {
        var request = new RunRequest { Input = "hello" };

        var id1 = SessionCoordinator.ResolveSessionId(request);
        var id2 = SessionCoordinator.ResolveSessionId(request);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void CreateRunId_GeneratesUniqueIds()
    {
        var runId1 = SessionCoordinator.CreateRunId();
        var runId2 = SessionCoordinator.CreateRunId();

        Assert.NotNull(runId1);
        Assert.NotEqual(runId1, runId2);
    }
}
