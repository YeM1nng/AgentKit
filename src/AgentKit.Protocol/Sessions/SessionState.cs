using System.Text.Json;

namespace AgentKit.Protocol.Sessions;

/// <summary>会话状态。</summary>
public sealed class SessionState
{
    /// <summary>会话 ID。</summary>
    public required string SessionId { get; init; }

    /// <summary>MAF AgentSession 序列化数据。</summary>
    public JsonElement? AgentSessionData { get; init; }

    /// <summary>恢复上下文。</summary>
    public ResumptionContext ResumptionContext { get; init; } = new();

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
