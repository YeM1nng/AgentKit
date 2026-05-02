namespace AgentKit.Protocol.Sessions;

/// <summary>恢复上下文，保存审批、序号、上一次运行等业务恢复信息。</summary>
public sealed class ResumptionContext
{
    /// <summary>待审批列表。</summary>
    public IReadOnlyList<PendingApproval> PendingApprovals { get; init; } = [];

    /// <summary>消息序号。</summary>
    public long Sequence { get; init; }

    /// <summary>上次运行 ID。</summary>
    public string? LastRunId { get; init; }

    /// <summary>是否有未持久化的脏数据。</summary>
    public bool IsDirty { get; init; }
}
