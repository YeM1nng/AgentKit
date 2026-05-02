using System.Text.Json;

using AgentKit.Protocol.Enums;

namespace AgentKit.Protocol.Results;

/// <summary>失败信息。</summary>
public sealed class Failure
{
    /// <summary>失败类型。</summary>
    public required FailureKind Kind { get; init; }

    /// <summary>失败消息。</summary>
    public required string Message { get; init; }

    /// <summary>失败详情。</summary>
    public string? Detail { get; init; }

    /// <summary>失败上下文。</summary>
    public JsonDocument? Context { get; init; }
}
