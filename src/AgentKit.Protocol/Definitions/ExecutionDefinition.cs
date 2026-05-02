using System.Text.Json;

namespace AgentKit.Protocol.Definitions;

/// <summary>执行策略定义。</summary>
public sealed class ExecutionDefinition
{
    /// <summary>是否启用流式运行。</summary>
    public bool StreamingEnabled { get; init; } = true;

    /// <summary>是否允许多工具并行调用。</summary>
    public bool AllowMultipleToolCalls { get; init; }

    /// <summary>是否允许后台响应。</summary>
    public bool? AllowBackgroundResponses { get; init; }

    /// <summary>是否启用逐次调用历史持久化。</summary>
    public bool PerServiceCallPersistence { get; init; }

    /// <summary>模型是否支持 Tools 与 Structured Output 并存。null 表示运行时自动探测。</summary>
    public bool? ModelSupportsToolsWithStructuredOutput { get; init; }

    /// <summary>重试策略。</summary>
    public RetryPolicyDefinition Retry { get; init; } = new();

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
