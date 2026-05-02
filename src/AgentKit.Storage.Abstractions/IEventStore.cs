using AgentKit.Storage.Models;

namespace AgentKit.Storage;

/// <summary>事件存储，append-only 语义。</summary>
public interface IEventStore
{
    /// <summary>追加事件。</summary>
    /// <param name="event">要追加的事件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AppendAsync(StoredEvent @event, CancellationToken cancellationToken);

    /// <summary>按运行 ID 列出事件。</summary>
    /// <param name="runId">运行 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>事件列表。</returns>
    Task<IReadOnlyList<StoredEvent>> ListAsync(string runId, CancellationToken cancellationToken);
}
