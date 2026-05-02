using AgentKit.Protocol.Results;

namespace AgentKit.Protocol.Events;

/// <summary>运行失败事件。</summary>
public sealed record RunFailedEvent : KitEvent
{
    /// <summary>失败信息。</summary>
    public required Failure Failure { get; init; }
}
