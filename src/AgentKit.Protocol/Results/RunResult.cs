using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Sessions;

namespace AgentKit.Protocol.Results;

/// <summary>运行结果。</summary>
public sealed class RunResult
{
    /// <summary>运行状态。</summary>
    public required RunState RunState { get; init; }

    /// <summary>最终文本输出。</summary>
    public string? FinalText { get; init; }

    /// <summary>结构化输出载荷。</summary>
    public JsonDocument? StructuredPayload { get; init; }

    /// <summary>结构化输出校验结果。</summary>
    public StructuredValidationResult? StructuredValidation { get; init; }

    /// <summary>会话状态。</summary>
    public SessionState? SessionState { get; init; }

    /// <summary>待审批项。</summary>
    public PendingApproval? PendingApproval { get; init; }

    /// <summary>续跑令牌。</summary>
    public string? ContinuationToken { get; init; }

    /// <summary>已使用的重试次数。</summary>
    public int AttemptsUsed { get; init; }

    /// <summary>失败信息。</summary>
    public Failure? Failure { get; init; }
}
