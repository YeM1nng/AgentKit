using System.Collections.Concurrent;

using AgentKit.Protocol.Definitions;

namespace AgentKit.Maf.Composition;

/// <summary>基于 Key + Version 缓存已解析的 Agent 定义。</summary>
public sealed class DefinitionCache
{
    private readonly ConcurrentDictionary<string, AgentKitDefinition> _cache = new(StringComparer.Ordinal);

    /// <summary>获取缓存的定义。</summary>
    /// <param name="key">定义键。</param>
    /// <param name="version">定义版本。</param>
    /// <returns>缓存的定义，未命中则返回 null。</returns>
    public AgentKitDefinition? Get(string key, string? version)
    {
        _cache.TryGetValue(GetCacheKey(key, version), out var definition);
        return definition;
    }

    /// <summary>缓存定义。</summary>
    /// <param name="key">定义键。</param>
    /// <param name="version">定义版本。</param>
    /// <param name="definition">要缓存的定义。</param>
    public void Set(string key, string? version, AgentKitDefinition definition)
    {
        _cache[GetCacheKey(key, version)] = definition;
    }

    /// <summary>清除全部缓存。</summary>
    public void Clear() => _cache.Clear();

    private static string GetCacheKey(string key, string? version) => $"{key}@{version ?? "*"}";
}
