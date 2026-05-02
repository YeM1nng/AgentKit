using System.Text.Json;

namespace AgentKit.Protocol.Requests;

/// <summary>消息内容基类。</summary>
public abstract record MessageContent
{
    /// <summary>内容类型标识。</summary>
    public required string ContentType { get; init; }
}

/// <summary>文本内容。</summary>
public sealed record AgentKitTextContent : MessageContent
{
    /// <summary>文本内容。</summary>
    public required string Text { get; init; }
}

/// <summary>函数调用内容。</summary>
public sealed record AgentKitFunctionCallContent : MessageContent
{
    /// <summary>调用 ID。</summary>
    public required string CallId { get; init; }

    /// <summary>函数名称。</summary>
    public required string Name { get; init; }

    /// <summary>调用参数。</summary>
    public required JsonDocument Arguments { get; init; }
}

/// <summary>函数调用结果内容。</summary>
public sealed record AgentKitFunctionResultContent : MessageContent
{
    /// <summary>调用 ID。</summary>
    public required string CallId { get; init; }

    /// <summary>函数名称。</summary>
    public required string Name { get; init; }

    /// <summary>调用结果。</summary>
    public required JsonDocument Result { get; init; }

    /// <summary>是否为错误结果。</summary>
    public bool IsError { get; init; }
}
