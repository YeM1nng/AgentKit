using AgentKit.Abstractions;
using AgentKit.Protocol.Definitions;
using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Requests;
using AgentKit.Protocol.Results;

namespace AgentKit.Core;

/// <summary>AgentKit 运行器，协调整个运行生命周期。</summary>
public sealed class Runner : IRunner
{
    private readonly IExecutionPipeline _executionPipeline;

    /// <summary>构造函数。</summary>
    /// <param name="executionPipeline">执行管道。</param>
    public Runner(IExecutionPipeline executionPipeline)
    {
        _executionPipeline = executionPipeline;
    }

    /// <inheritdoc />
    public async Task<RunResult> RunAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken)
    {
        var definitionErrors = DefinitionValidator.Validate(definition);
        if (definitionErrors.Count > 0)
        {
            return new RunResult
            {
                RunState = RunState.Failed,
                Failure = new Failure
                {
                    Kind = FailureKind.ConfigurationInvalid,
                    Message = "Agent 定义校验失败。",
                    Detail = string.Join("; ", definitionErrors),
                },
            };
        }

        var requestErrors = RunRequestValidator.Validate(request);
        if (requestErrors.Count > 0)
        {
            return new RunResult
            {
                RunState = RunState.Failed,
                Failure = new Failure
                {
                    Kind = FailureKind.ConfigurationInvalid,
                    Message = "运行请求校验失败。",
                    Detail = string.Join("; ", requestErrors),
                },
            };
        }

        var pipelineResult = await _executionPipeline.RunAsync(definition, request, cancellationToken);

        return ResultAggregator.Aggregate(pipelineResult);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<KitEvent> StreamAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken)
    {
        var definitionErrors = DefinitionValidator.Validate(definition);
        if (definitionErrors.Count > 0)
            throw new InvalidOperationException($"Agent 定义校验失败：{string.Join("; ", definitionErrors)}");

        var requestErrors = RunRequestValidator.Validate(request);
        if (requestErrors.Count > 0)
            throw new InvalidOperationException($"运行请求校验失败：{string.Join("; ", requestErrors)}");

        return _executionPipeline.StreamAsync(definition, request, cancellationToken);
    }
}
