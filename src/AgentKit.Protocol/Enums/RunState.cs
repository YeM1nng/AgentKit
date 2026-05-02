namespace AgentKit.Protocol.Enums;

/// <summary>运行状态。</summary>
public enum RunState
{
    /// <summary>运行中。</summary>
    Running,

    /// <summary>正常完成。</summary>
    Completed,

    /// <summary>完成并返回了工具调用，等待下一轮执行。</summary>
    CompletedWithToolCalls,

    /// <summary>运行中断，等待审批。</summary>
    CompletedWithApproval,

    /// <summary>需要续跑（后台响应）。</summary>
    CompletedWithContinuation,

    /// <summary>运行失败。</summary>
    Failed,

    /// <summary>内容过滤触发。</summary>
    FailedContentFilter,

    /// <summary>Token 超限。</summary>
    FailedTokenLimit
}
