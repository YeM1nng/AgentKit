using System.Text.Json;

using AgentKit.Protocol.Definitions;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Requests;
using AgentKit.Protocol.Results;
using AgentKit.Protocol.Sessions;

namespace AgentKit.Core;

/// <summary>执行管道接口，封装 MAF 特定的运行逻辑。</summary>
/// <remarks>
/// 由 AgentKit.Maf 层实现，通过 DI 注入到 Core 层。
/// Core 层通过此接口委托 MAF 特定操作，避免直接引用 Maf 项目。
/// </remarks>
public interface IExecutionPipeline
{
    /// <summary>非流式运行。</summary>
    /// <param name="definition">Agent 定义。</param>
    /// <param name="request">运行请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>运行结果与事件列表。</returns>
    Task<PipelineResult> RunAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken);

    /// <summary>流式运行。</summary>
    /// <param name="definition">Agent 定义。</param>
    /// <param name="request">运行请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>事件异步流。</returns>
    IAsyncEnumerable<KitEvent> StreamAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken);
}

/// <summary>管道执行结果。</summary>
/// <param name="FinalText">最终文本输出。</param>
/// <param name="SessionState">会话状态。</param>
/// <param name="Events">事件列表。</param>
/// <param name="StructuredValidation">结构化输出校验结果，无则为 null。</param>
/// <param name="StructuredPayload">反序列化后的结构化载荷。</param>
/// <param name="ContinuationToken">续跑令牌，无则为 null。</param>
/// <param name="AttemptsUsed">实际使用的重试次数。</param>
public sealed record PipelineResult(
    string? FinalText,
    SessionState? SessionState,
    IReadOnlyList<KitEvent> Events,
    StructuredValidationResult? StructuredValidation = null,
    JsonDocument? StructuredPayload = null,
    string? ContinuationToken = null,
    int AttemptsUsed = 1);
