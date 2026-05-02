using System.Text.Json;

namespace AgentKit.Protocol.Events;

/// <summary>事件基类。</summary>
public abstract record KitEvent
{
    /// <summary>事件类型标识。</summary>
    public required string EventType { get; init; }

    /// <summary>运行 ID。</summary>
    public required string RunId { get; init; }

    /// <summary>会话 ID。</summary>
    public required string SessionId { get; init; }

    /// <summary>事件序号，单调递增。</summary>
    public required long Sequence { get; init; }

    /// <summary>重试次数。</summary>
    public required int Attempt { get; init; }

    /// <summary>事件发生时间。</summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
