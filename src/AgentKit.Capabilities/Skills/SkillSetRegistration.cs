using System.Text.Json;

using AgentKit.Protocol.Definitions;

namespace AgentKit.Capabilities.Skills;

/// <summary>技能集注册信息。</summary>
public sealed class SkillSetRegistration
{
    /// <summary>技能集注册键。</summary>
    public required string Key { get; init; }

    /// <summary>技能文件路径列表。</summary>
    public required IReadOnlyList<string> Paths { get; init; }

    /// <summary>自定义 Prompt 模板。</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>是否需要脚本执行审批。</summary>
    public bool ScriptApproval { get; init; }

    /// <summary>是否禁用缓存。</summary>
    public bool DisableCaching { get; init; }

    /// <summary>脚本运行器委托。</summary>
    public Func<string, Task<string>>? ScriptRunner { get; init; }

    /// <summary>文件发现选项。</summary>
    public SkillFileSourceOptions? FileSourceOptions { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
