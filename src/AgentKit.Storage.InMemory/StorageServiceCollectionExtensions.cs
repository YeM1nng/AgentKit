using Microsoft.Extensions.DependencyInjection;

namespace AgentKit.Storage.InMemory;

/// <summary>存储 DI 注册扩展。</summary>
public static class StorageServiceCollectionExtensions
{
    /// <summary>注册 InMemory 存储实现。</summary>
    public static IServiceCollection AddAgentKitInMemoryStorage(this IServiceCollection services)
    {
        var store = new InMemoryStore();
        services.AddSingleton<ISessionStore>(store);
        services.AddSingleton<IEventStore>(store);
        services.AddSingleton<IApprovalStore>(store);
        return services;
    }
}
