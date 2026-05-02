namespace AgentKit.Protocol.Definitions;

/// <summary>重试策略定义。</summary>
public sealed class RetryPolicyDefinition
{
    /// <summary>是否启用重试。</summary>
    public bool Enabled { get; init; }

    /// <summary>最大重试次数。</summary>
    public int MaxAttempts { get; init; } = 1;

    /// <summary>重试基础延迟。</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>是否在结构化输出收尾失败时重试。</summary>
    public bool RetryStructuredFinalization { get; init; } = true;

    /// <summary>是否仅在未发出用户可见文本时允许重试。</summary>
    public bool RetryBeforeVisibleOutputOnly { get; init; } = true;
}
