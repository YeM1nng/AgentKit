using AgentKit.Abstractions;
using AgentKit.Protocol.Definitions;

using Microsoft.Extensions.AI;

namespace AgentKit.Extensions.DependencyInjection;

/// <summary>基于 provider 工厂注册的模型客户端解析器。</summary>
public sealed class ProviderModelClientResolver : IModelClientResolver
{
    private readonly Dictionary<string, Func<ModelDefinition, CancellationToken, Task<IChatClient>>> _factories;

    /// <summary>构造函数。</summary>
    /// <param name="factories">provider 名称 → 工厂函数 映射。</param>
    public ProviderModelClientResolver(
        Dictionary<string, Func<ModelDefinition, CancellationToken, Task<IChatClient>>> factories)
    {
        _factories = factories;
    }

    /// <inheritdoc />
    public Task<IChatClient> ResolveAsync(ModelDefinition model, CancellationToken cancellationToken)
    {
        if (_factories.TryGetValue(model.Provider, out var factory))
            return factory(model, cancellationToken);

        throw new InvalidOperationException($"未注册模型提供者 '{model.Provider}'。");
    }
}
