using System.Text.Json;

namespace AgentKit.Protocol.Definitions;

/// <summary>技能集定义。</summary>
public sealed class SkillSetDefinition
{
    /// <summary>技能集注册键。</summary>
    public required string Key { get; init; }

    /// <summary>技能文件路径列表。</summary>
    public IReadOnlyList<string> Paths { get; init; } = [];

    /// <summary>是否需要脚本执行审批。</summary>
    public bool ScriptApproval { get; init; }

    /// <summary>是否禁用缓存。</summary>
    public bool DisableCaching { get; init; }

    /// <summary>自定义 Prompt 模板。</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>文件发现选项。</summary>
    public SkillFileSourceOptions? FileSourceOptions { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
