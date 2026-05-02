using System.Text.Json;

namespace AgentKit.Storage.Models;

/// <summary>持久化存储的事件记录。</summary>
public sealed class StoredEvent
{
    /// <summary>运行 ID。</summary>
    public required string RunId { get; init; }

    /// <summary>会话 ID。</summary>
    public required string SessionId { get; init; }

    /// <summary>事件序号。</summary>
    public required long Sequence { get; init; }

    /// <summary>事件类型。</summary>
    public required string EventType { get; init; }

    /// <summary>事件负载（JSON）。</summary>
    public required JsonDocument Payload { get; init; }

    /// <summary>重试次数。</summary>
    public int Attempt { get; init; }

    /// <summary>发生时间（UTC）。</summary>
    public DateTimeOffset OccurredAtUtc { get; init; }
}
