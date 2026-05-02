using System.Text.Json;

namespace AgentKit.Storage.Models;

/// <summary>持久化存储的会话快照。</summary>
public sealed class StoredSession
{
    /// <summary>会话 ID。</summary>
    public required string SessionId { get; init; }

    /// <summary>定义键。</summary>
    public required string DefinitionKey { get; init; }

    /// <summary>定义版本。</summary>
    public string? DefinitionVersion { get; init; }

    /// <summary>Agent 会话数据（MAF 序列化快照）。</summary>
    public JsonElement? AgentSessionData { get; init; }

    /// <summary>恢复上下文（JSON 序列化）。</summary>
    public JsonElement? ResumptionContext { get; init; }

    /// <summary>乐观并发版本号。</summary>
    public long Version { get; init; }

    /// <summary>最后更新时间（UTC）。</summary>
    public DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
