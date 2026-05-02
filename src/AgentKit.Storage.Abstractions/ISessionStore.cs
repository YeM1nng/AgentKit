using AgentKit.Storage.Models;

namespace AgentKit.Storage;

/// <summary>会话存储，管理 Session 快照与消息历史。</summary>
public interface ISessionStore
{
    /// <summary>获取会话快照。</summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话快照，不存在时返回 null。</returns>
    Task<StoredSession?> GetAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>保存会话快照。</summary>
    /// <param name="session">会话快照。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(StoredSession session, CancellationToken cancellationToken);

    /// <summary>加载会话的全部消息历史。</summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>消息列表。</returns>
    Task<IReadOnlyList<StoredMessage>> LoadMessagesAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>追加消息到会话历史。</summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="messages">要追加的消息列表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AppendMessagesAsync(string sessionId, IReadOnlyList<StoredMessage> messages, CancellationToken cancellationToken);
}
