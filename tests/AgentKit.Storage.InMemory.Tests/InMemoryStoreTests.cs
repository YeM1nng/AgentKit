using System.Text.Json;

using AgentKit.Storage.Models;

using Xunit;

namespace AgentKit.Storage.InMemory.Tests;

/// <summary>内存存储测试。</summary>
public class InMemoryStoreTests
{
    private readonly InMemoryStore _store = new();

    [Fact]
    public async Task Session_SaveAndGet_Succeeds()
    {
        var session = new StoredSession
        {
            SessionId = "sess-1",
            DefinitionKey = "agent-1",
            DefinitionVersion = "1.0.0",
        };

        await _store.SaveAsync(session, CancellationToken.None);
        var loaded = await _store.GetAsync("sess-1", CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("sess-1", loaded.SessionId);
        Assert.Equal("agent-1", loaded.DefinitionKey);
    }

    [Fact]
    public async Task Session_GetNonExistent_ReturnsNull()
    {
        var result = await _store.GetAsync("non-existent", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Session_SaveOverwrites_Succeeds()
    {
        var session1 = new StoredSession
        {
            SessionId = "sess-1",
            DefinitionKey = "agent-1",
            Version = 1,
        };
        var session2 = new StoredSession
        {
            SessionId = "sess-1",
            DefinitionKey = "agent-1",
            Version = 2,
        };

        await _store.SaveAsync(session1, CancellationToken.None);
        await _store.SaveAsync(session2, CancellationToken.None);
        var loaded = await _store.GetAsync("sess-1", CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Version);
    }

    [Fact]
    public async Task Message_AppendAndLoad_Succeeds()
    {
        var messages = new List<StoredMessage>
        {
            new() { SessionId = "sess-1", Sequence = 1, Role = MessageRole.User, CreatedAtUtc = DateTimeOffset.UtcNow },
            new() { SessionId = "sess-1", Sequence = 2, Role = MessageRole.Assistant, CreatedAtUtc = DateTimeOffset.UtcNow },
        };

        await _store.AppendMessagesAsync("sess-1", messages, CancellationToken.None);
        var loaded = await _store.LoadMessagesAsync("sess-1", CancellationToken.None);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(1, loaded[0].Sequence);
        Assert.Equal(2, loaded[1].Sequence);
    }

    [Fact]
    public async Task Message_LoadEmpty_ReturnsEmpty()
    {
        var loaded = await _store.LoadMessagesAsync("non-existent", CancellationToken.None);
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task Message_AppendAccumulates_Succeeds()
    {
        await _store.AppendMessagesAsync("sess-1",
            [new StoredMessage { SessionId = "sess-1", Sequence = 1, Role = MessageRole.User, CreatedAtUtc = DateTimeOffset.UtcNow }],
            CancellationToken.None);

        await _store.AppendMessagesAsync("sess-1",
            [new StoredMessage { SessionId = "sess-1", Sequence = 2, Role = MessageRole.Assistant, CreatedAtUtc = DateTimeOffset.UtcNow }],
            CancellationToken.None);

        var loaded = await _store.LoadMessagesAsync("sess-1", CancellationToken.None);
        Assert.Equal(2, loaded.Count);
    }

    [Fact]
    public async Task Event_AppendAndList_Succeeds()
    {
        var events = new[]
        {
            new StoredEvent { RunId = "run-1", SessionId = "sess-1", Sequence = 0, EventType = "Started", Payload = JsonDocument.Parse("{}"), OccurredAtUtc = DateTimeOffset.UtcNow },
            new StoredEvent { RunId = "run-1", SessionId = "sess-1", Sequence = 1, EventType = "Completed", Payload = JsonDocument.Parse("{}"), OccurredAtUtc = DateTimeOffset.UtcNow },
        };

        foreach (var ev in events)
            await _store.AppendAsync(ev, CancellationToken.None);

        var listed = await _store.ListAsync("run-1", CancellationToken.None);
        Assert.Equal(2, listed.Count);
        Assert.Equal("Started", listed[0].EventType);
        Assert.Equal("Completed", listed[1].EventType);
    }

    [Fact]
    public async Task Event_ListEmpty_ReturnsEmpty()
    {
        var listed = await _store.ListAsync("non-existent", CancellationToken.None);
        Assert.Empty(listed);
    }

    [Fact]
    public async Task Approval_CreateAndGet_Succeeds()
    {
        var approval = new StoredApproval
        {
            RequestId = "req-1",
            RunId = "run-1",
            SessionId = "sess-1",
            TargetKind = ApprovalTargetKind.FunctionTool,
            TargetName = "delete-file",
            Arguments = JsonDocument.Parse("{\"path\": \"/tmp\"}"),
        };

        await _store.CreateAsync(approval, CancellationToken.None);
        var loaded = await _store.GetByRequestIdAsync("req-1", CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("req-1", loaded.RequestId);
        Assert.Equal("delete-file", loaded.TargetName);
        Assert.Equal("pending", loaded.Status);
    }

    [Fact]
    public async Task Approval_GetNonExistent_ReturnsNull()
    {
        var result = await _store.GetByRequestIdAsync("non-existent", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Approval_SaveDecision_Succeeds()
    {
        var decision = new StoredApprovalDecision
        {
            RequestId = "req-1",
            Approved = true,
            Comment = "Approved",
            DecidedBy = "admin",
            DecidedAtUtc = DateTimeOffset.UtcNow,
        };

        await _store.SaveDecisionAsync(decision, CancellationToken.None);
    }
}
