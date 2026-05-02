using System.Collections.Concurrent;

using AgentKit.Abstractions;
using AgentKit.Protocol.Definitions;

namespace AgentKit.Core;

/// <summary>基于反射的结构化类型解析器实现。</summary>
/// <remarks>从已加载的程序集中搜索目标类型。若定义中指定了 TargetTypeName，则用于反序列化结构化输出。</remarks>
public sealed class StructuredTypeResolver : IStructuredTypeResolver
{
    private readonly ConcurrentDictionary<string, Type> _cache = new(StringComparer.Ordinal);

    /// <summary>尝试解析类型。</summary>
    /// <param name="targetTypeName">目标类型全名。</param>
    /// <param name="type">解析出的类型，失败时为 null。</param>
    /// <returns>是否解析成功。</returns>
    public bool TryResolve(string targetTypeName, out Type? type)
    {
        if (_cache.TryGetValue(targetTypeName, out type))
            return true;

        type = Type.GetType(targetTypeName);
        if (type is not null)
        {
            _cache[targetTypeName] = type;
            return true;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(targetTypeName);
            if (type is not null)
            {
                _cache[targetTypeName] = type;
                return true;
            }
        }

        return false;
    }

    /// <summary>注册已知类型，避免运行时反射。</summary>
    /// <param name="typeName">类型名称。</param>
    /// <param name="type">类型实例。</param>
    public void Register(string typeName, Type type) => _cache[typeName] = type;
}
