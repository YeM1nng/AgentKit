using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Results;
using AgentKit.Protocol.Sessions;

namespace AgentKit.Core;

/// <summary>聚合运行结果，从事件流与最终响应中提取 RunResult 所需字段。</summary>
public static class ResultAggregator
{
    /// <summary>从管道结果聚合运行结果。</summary>
    /// <param name="pipelineResult">管道执行结果。</param>
    /// <returns>运行结果。</returns>
    public static RunResult Aggregate(PipelineResult pipelineResult)
    {
        return Aggregate(
            pipelineResult.Events,
            pipelineResult.FinalText,
            pipelineResult.SessionState,
            pipelineResult.StructuredValidation,
            pipelineResult.StructuredPayload,
            pipelineResult.ContinuationToken,
            pipelineResult.AttemptsUsed);
    }

    /// <summary>从事件流聚合运行结果。</summary>
    /// <param name="events">事件列表。</param>
    /// <param name="finalText">最终文本输出。</param>
    /// <param name="sessionState">会话状态。</param>
    /// <param name="structuredValidation">结构化输出校验结果。</param>
    /// <param name="structuredPayload">结构化输出载荷。</param>
    /// <param name="continuationToken">续跑令牌。</param>
    /// <param name="attemptsUsed">实际使用的重试次数。</param>
    /// <returns>运行结果。</returns>
    public static RunResult Aggregate(
        IReadOnlyList<KitEvent> events,
        string? finalText,
        SessionState? sessionState,
        StructuredValidationResult? structuredValidation = null,
        JsonDocument? structuredPayload = null,
        string? continuationToken = null,
        int attemptsUsed = 1)
    {
        bool hasToolCalls = false, hasApproval = false;
        RunFailedEvent? lastFailure = null;
        PendingApproval? pendingApproval = null;

        foreach (var ev in events)
        {
            switch (ev)
            {
                case ToolInvokedEvent: hasToolCalls = true; break;
                case ApprovalRequiredEvent ae:
                    hasApproval = true;
                    pendingApproval = ae.Approval;
                    break;
                case RunFailedEvent fe: lastFailure = fe; break;
            }
        }

        RunState state;
        Failure? failure = null;

        if (lastFailure is not null)
        {
            state = RunState.Failed;
            failure = lastFailure.Failure;
        }
        else if (structuredValidation is { IsValid: false })
        {
            state = RunState.Failed;
            failure = new Failure
            {
                Kind = FailureKind.StructuredOutputValidationFailed,
                Message = "结构化输出校验失败。",
                Detail = structuredValidation.Errors is not null
                    ? string.Join("; ", structuredValidation.Errors)
                    : "结构化输出校验失败",
            };
        }
        else if (hasApproval)
        {
            state = RunState.CompletedWithApproval;
        }
        else if (continuationToken is not null)
        {
            state = RunState.CompletedWithContinuation;
        }
        else if (hasToolCalls)
        {
            state = RunState.CompletedWithToolCalls;
        }
        else
        {
            state = RunState.Completed;
        }

        if (structuredPayload is null
            && structuredValidation is { IsValid: true, RawOutput: not null })
        {
            structuredPayload = JsonDocument.Parse(structuredValidation.RawOutput);
        }

        return new RunResult
        {
            RunState = state,
            FinalText = finalText,
            SessionState = sessionState,
            Failure = failure,
            StructuredValidation = structuredValidation,
            StructuredPayload = structuredPayload,
            PendingApproval = pendingApproval,
            ContinuationToken = continuationToken,
            AttemptsUsed = attemptsUsed,
        };
    }
}
