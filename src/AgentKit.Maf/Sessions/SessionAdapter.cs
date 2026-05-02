using System.Text.Json;

using Microsoft.Agents.AI;

namespace AgentKit.Maf.Sessions;

/// <summary>管理 MAF AgentSession 的创建、恢复与序列化。</summary>
public sealed class SessionAdapter
{
    /// <summary>创建新会话。</summary>
    /// <param name="agent">MAF Agent 实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新创建的会话。</returns>
    public async Task<AgentSession> CreateAsync(AIAgent agent, CancellationToken cancellationToken)
    {
        return await agent.CreateSessionAsync(cancellationToken);
    }

    /// <summary>从存储快照恢复会话。</summary>
    /// <param name="agent">MAF Agent 实例。</param>
    /// <param name="serializedState">序列化的会话状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>恢复的会话。</returns>
    public async Task<AgentSession> RestoreAsync(AIAgent agent, JsonElement serializedState, CancellationToken cancellationToken)
    {
        return await agent.DeserializeSessionAsync(serializedState, cancellationToken: cancellationToken);
    }

    /// <summary>序列化会话为可存储格式。</summary>
    /// <param name="agent">MAF Agent 实例。</param>
    /// <param name="session">要序列化的会话。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>序列化的 JSON 元素。</returns>
    public async Task<JsonElement> SerializeAsync(AIAgent agent, AgentSession session, CancellationToken cancellationToken)
    {
        return await agent.SerializeSessionAsync(session, jsonSerializerOptions: null, cancellationToken: cancellationToken);
    }
}
