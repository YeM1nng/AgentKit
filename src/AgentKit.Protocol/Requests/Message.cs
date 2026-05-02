using System.Text.Json;

using AgentKit.Protocol.Enums;

namespace AgentKit.Protocol.Requests;

/// <summary>输入消息。</summary>
public sealed class Message
{
    /// <summary>消息角色。</summary>
    public required MessageRole Role { get; init; }

    /// <summary>文本内容，Contents 为单文本时的快捷方式。</summary>
    public string? Text { get; init; }

    /// <summary>多模态内容列表。</summary>
    public IReadOnlyList<MessageContent>? Contents { get; init; }

    /// <summary>消息来源标识。</summary>
    public string? Source { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
