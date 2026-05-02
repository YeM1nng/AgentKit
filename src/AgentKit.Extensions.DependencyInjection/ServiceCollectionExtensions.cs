using AgentKit.Abstractions;
using AgentKit.Core;

using AgentKit.Maf.Composition;
using AgentKit.Maf.Events;
using AgentKit.Maf.Execution;
using AgentKit.Maf.Sessions;
using AgentKit.Protocol.Definitions;
using AgentKit.Storage;
using AgentKit.Storage.InMemory;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentKit.Extensions.DependencyInjection;

/// <summary>AgentKit DI 注册扩展。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>注册 AgentKit 核心服务。</summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddAgentKitCore(this IServiceCollection services)
    {
        services.AddSingleton<Runner>();
        services.AddSingleton<IRunner>(sp => sp.GetRequiredService<Runner>());
        return services;
    }

    /// <summary>注册 AgentKit MAF 适配层。</summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddAgentKitMaf(this IServiceCollection services)
    {
        services.AddSingleton<Composer>(sp =>
            new Composer(sp, sp.GetService<ILoggerFactory>()));

        services.AddSingleton<SessionAdapter>();
        services.AddSingleton<EventProjector>();

        services.AddSingleton<IExecutionPipeline>(sp =>
            new MafExecutionPipeline(
                sp.GetRequiredService<Composer>(),
                sp.GetRequiredService<SessionAdapter>(),
                sp.GetRequiredService<EventProjector>(),
                sp.GetRequiredService<IModelClientResolver>(),
                sp.GetRequiredService<ISessionStore>(),
                sp.GetService<IApprovalStore>(),
                sp.GetService<IEventStore>(),
                sp.GetService<IStructuredTypeResolver>(),
                sp.GetService<IStructuredOutputRepairStrategy>()));

        return services;
    }

    /// <summary>注册模型提供者工厂。</summary>
    /// <param name="services">服务集合。</param>
    /// <param name="provider">提供者名称，与 <see cref="ModelDefinition.Provider"/> 匹配。</param>
    /// <param name="factory">工厂函数，接收 <see cref="ModelDefinition"/> 返回 <see cref="IChatClient"/>。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddModelProvider(
        this IServiceCollection services,
        string provider,
        Func<ModelDefinition, CancellationToken, Task<IChatClient>> factory)
    {
        services.Configure<ModelProviderOptions>(opts => opts.Factories[provider] = factory);
        return services;
    }

    /// <summary>注册基于 provider 工厂的模型客户端解析器。</summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddAgentKitModelResolution(this IServiceCollection services)
    {
        services.AddSingleton<IModelClientResolver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ModelProviderOptions>>().Value;
            return new ProviderModelClientResolver(options.Factories);
        });
        return services;
    }

    /// <summary>组合注册 AgentKit 全部服务，包括核心、MAF 适配、InMemory 存储与模型解析。</summary>
    /// <remarks>
    /// 内置注册 OpenAI 提供者，通过 <see cref="ModelDefinition.CredentialKey"/> 传入 API Key。
    /// 可选注册 <see cref="IStructuredTypeResolver"/>、<see cref="IStructuredOutputRepairStrategy"/>。
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddAgentKit(this IServiceCollection services)
    {
        services.AddAgentKitCore();
        services.AddAgentKitMaf();
        services.AddAgentKitInMemoryStorage();

        services.AddModelProvider("openai", static (model, _) =>
        {
            var apiKey = model.CredentialKey
                ?? throw new InvalidOperationException("OpenAI 需要设置 ModelDefinition.CredentialKey 作为 API Key。");

            var credential = new System.ClientModel.ApiKeyCredential(apiKey);
            var options = model.Endpoint is not null
                ? new OpenAI.OpenAIClientOptions { Endpoint = new Uri(model.Endpoint) }
                : null;

            var client = options is null
                ? new OpenAI.OpenAIClient(credential)
                : new OpenAI.OpenAIClient(credential, options);

            return Task.FromResult<IChatClient>(client.GetChatClient(model.ModelId).AsIChatClient());
        });

        services.AddAgentKitModelResolution();

        return services;
    }
}
