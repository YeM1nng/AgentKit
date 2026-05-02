using AgentKit.Protocol.Definitions;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentKit.Maf.Composition;

/// <summary>从 AgentKit 定义组装 ChatClientAgent。</summary>
public sealed class Composer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>构造函数。</summary>
    /// <param name="serviceProvider">服务提供者，用于工具工厂解析依赖。</param>
    /// <param name="loggerFactory">日志工厂。</param>
    public Composer(IServiceProvider serviceProvider, ILoggerFactory? loggerFactory = null)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    /// <summary>组装 Agent，返回组装结果。</summary>
    /// <param name="definition">Agent 定义。</param>
    /// <param name="chatClient">聊天客户端。</param>
    /// <param name="chatHistoryProvider">历史消息提供器。</param>
    /// <param name="contextProviders">上下文注入提供器列表。</param>
    /// <returns>组装结果。</returns>
    public ComposedAgent Compose(
        AgentKitDefinition definition,
        IChatClient chatClient,
        ChatHistoryProvider? chatHistoryProvider = null,
        IEnumerable<AIContextProvider>? contextProviders = null)
    {
        var chatOptions = CreateChatOptions(definition);

        var options = new ChatClientAgentOptions
        {
            Id = $"{definition.Key}:{definition.Version ?? "latest"}",
            Name = definition.Name,
            Description = definition.Description,
            ChatOptions = chatOptions,
            ChatHistoryProvider = chatHistoryProvider,
            AIContextProviders = contextProviders,
            RequirePerServiceCallChatHistoryPersistence = definition.Execution.PerServiceCallPersistence,
        };

        var agent = new ChatClientAgent(chatClient, options, _loggerFactory, _serviceProvider);

        return new ComposedAgent(agent, chatOptions);
    }

    private static ChatOptions CreateChatOptions(AgentKitDefinition definition)
    {
        var options = new ChatOptions();

        if (definition.Model.Temperature.HasValue)
            options.Temperature = (float)definition.Model.Temperature.Value;

        if (definition.Model.TopP.HasValue)
            options.TopP = (float)definition.Model.TopP.Value;

        if (definition.Model.MaxOutputTokens.HasValue)
            options.MaxOutputTokens = definition.Model.MaxOutputTokens.Value;

        return options;
    }
}

/// <summary>组装结果，包含 Agent 实例与运行时所需的基础配置。</summary>
public sealed record ComposedAgent(
    ChatClientAgent Agent,
    ChatOptions BaseChatOptions);
