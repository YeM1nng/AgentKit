using System.Text.Json;

namespace AgentKit.Storage.Models;

/// <summary>持久化存储的消息记录。</summary>
public sealed class StoredMessage
{
    /// <summary>会话 ID。</summary>
    public required string SessionId { get; init; }

    /// <summary>消息序号。</summary>
    public required long Sequence { get; init; }

    /// <summary>消息角色。</summary>
    public required MessageRole Role { get; init; }

    /// <summary>消息内容（JSON 序列化）。</summary>
    public JsonElement? Contents { get; init; }

    /// <summary>消息来源标识。</summary>
    public string? Source { get; init; }

    /// <summary>创建时间（UTC）。</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}
