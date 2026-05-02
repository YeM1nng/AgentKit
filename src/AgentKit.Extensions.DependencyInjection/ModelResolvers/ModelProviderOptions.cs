using AgentKit.Protocol.Definitions;

using Microsoft.Extensions.AI;

namespace AgentKit.Extensions.DependencyInjection;

/// <summary>模型提供者工厂注册选项。</summary>
public sealed class ModelProviderOptions
{
    /// <summary>provider 名称 → 工厂函数 映射。</summary>
    public Dictionary<string, Func<ModelDefinition, CancellationToken, Task<IChatClient>>> Factories { get; } = new();
}
