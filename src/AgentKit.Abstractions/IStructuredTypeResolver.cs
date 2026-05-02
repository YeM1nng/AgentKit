namespace AgentKit.Abstractions;

/// <summary>将 TargetTypeName 字符串解析为运行时类型。</summary>
public interface IStructuredTypeResolver
{
    /// <summary>尝试解析类型。</summary>
    /// <param name="targetTypeName">目标类型全名。</param>
    /// <param name="type">解析出的类型，失败时为 null。</param>
    /// <returns>是否解析成功。</returns>
    bool TryResolve(string targetTypeName, out Type? type);
}
