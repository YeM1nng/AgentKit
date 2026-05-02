using AgentKit.Protocol.Enums;

namespace AgentKit.Protocol.Results;

/// <summary>结构化输出校验结果。</summary>
public sealed class StructuredValidationResult
{
    /// <summary>是否校验通过。</summary>
    public bool IsValid { get; init; }

    /// <summary>失败类型。</summary>
    public StructuredFailureKind? FailureKind { get; init; }

    /// <summary>校验错误列表。</summary>
    public IReadOnlyList<string>? Errors { get; init; }

    /// <summary>原始输出文本。</summary>
    public string? RawOutput { get; init; }
}
