namespace AgentKit.Protocol.Events;

/// <summary>响应增量事件。</summary>
public sealed record ResponseDeltaEvent : KitEvent
{
    /// <summary>文本增量内容。</summary>
    public required string TextDelta { get; init; }
}
