using System.Text.Json;

using AgentKit.Protocol.Sessions;

namespace AgentKit.Protocol.Requests;

/// <summary>运行请求。</summary>
public sealed class RunRequest
{
    /// <summary>输入文本，与 Messages 二选一。</summary>
    public string? Input { get; init; }

    /// <summary>输入消息列表。</summary>
    public IReadOnlyList<Message> Messages { get; init; } = [];

    /// <summary>会话状态，用于恢复已有会话。</summary>
    public SessionState? Session { get; init; }

    /// <summary>审批决策，用于审批恢复场景。</summary>
    public ApprovalDecision? ApprovalDecision { get; init; }

    /// <summary>续跑令牌。</summary>
    public string? ContinuationToken { get; init; }

    /// <summary>上下文数据。</summary>
    public JsonDocument? Context { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
