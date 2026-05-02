using AgentKit.Protocol.Definitions;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Requests;
using AgentKit.Protocol.Results;

namespace AgentKit.Abstractions;

/// <summary>Agent 运行器，负责执行 Agent 定义与运行请求的完整生命周期。</summary>
public interface IRunner
{
    /// <summary>非流式运行。</summary>
    /// <param name="definition">Agent 定义。</param>
    /// <param name="request">运行请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>运行结果。</returns>
    Task<RunResult> RunAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken = default);

    /// <summary>流式运行，返回统一事件流。</summary>
    /// <param name="definition">Agent 定义。</param>
    /// <param name="request">运行请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>事件异步流。</returns>
    IAsyncEnumerable<KitEvent> StreamAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken = default);
}
