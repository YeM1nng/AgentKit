using AgentKit.Protocol.Definitions;
using Microsoft.Extensions.AI;

namespace AgentKit.Abstractions;

/// <summary>根据模型定义解析出可用的 IChatClient。</summary>
public interface IModelClientResolver
{
    /// <summary>解析模型客户端。</summary>
    /// <param name="model">模型定义。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>聊天客户端实例。</returns>
    Task<IChatClient> ResolveAsync(ModelDefinition model, CancellationToken cancellationToken);
}
