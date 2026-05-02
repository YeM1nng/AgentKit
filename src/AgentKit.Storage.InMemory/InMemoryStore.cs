using System.Collections.Concurrent;

using AgentKit.Storage.Models;

namespace AgentKit.Storage.InMemory;

/// <summary>内存存储实现，同时实现三个存储接口。用于单元测试与本地调试。</summary>
public sealed class InMemoryStore : ISessionStore, IEventStore, IApprovalStore
{
    private readonly ConcurrentDictionary<string, StoredSession> _sessions = new();
    private readonly ConcurrentDictionary<string, List<StoredMessage>> _messages = new();
    private readonly ConcurrentDictionary<string, List<StoredEvent>> _events = new();
    private readonly ConcurrentDictionary<string, StoredApproval> _approvals = new();
    private readonly ConcurrentDictionary<string, StoredApprovalDecision> _decisions = new();

    /// <inheritdoc />
    public Task<StoredSession?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    /// <inheritdoc />
    public Task SaveAsync(StoredSession session, CancellationToken cancellationToken)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StoredMessage>> LoadMessagesAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (_messages.TryGetValue(sessionId, out var messages))
        {
            lock (messages)
            {
                return Task.FromResult<IReadOnlyList<StoredMessage>>(messages.ToList());
            }
        }

        return Task.FromResult<IReadOnlyList<StoredMessage>>([]);
    }

    /// <inheritdoc />
    public Task AppendMessagesAsync(string sessionId, IReadOnlyList<StoredMessage> messages, CancellationToken cancellationToken)
    {
        var list = _messages.GetOrAdd(sessionId, _ => []);
        lock (list)
        {
            list.AddRange(messages);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AppendAsync(StoredEvent @event, CancellationToken cancellationToken)
    {
        var list = _events.GetOrAdd(@event.RunId, _ => []);
        lock (list)
        {
            list.Add(@event);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StoredEvent>> ListAsync(string runId, CancellationToken cancellationToken)
    {
        if (_events.TryGetValue(runId, out var events))
        {
            lock (events)
            {
                return Task.FromResult<IReadOnlyList<StoredEvent>>(events.ToList());
            }
        }

        return Task.FromResult<IReadOnlyList<StoredEvent>>([]);
    }

    /// <inheritdoc />
    public Task CreateAsync(StoredApproval approval, CancellationToken cancellationToken)
    {
        _approvals[approval.RequestId] = approval;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<StoredApproval?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken)
    {
        _approvals.TryGetValue(requestId, out var approval);
        return Task.FromResult(approval);
    }

    /// <inheritdoc />
    public Task SaveDecisionAsync(StoredApprovalDecision decision, CancellationToken cancellationToken)
    {
        _decisions[decision.RequestId] = decision;
        return Task.CompletedTask;
    }
}
