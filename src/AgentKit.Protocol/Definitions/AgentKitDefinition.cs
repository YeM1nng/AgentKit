using System.Text.Json;

namespace AgentKit.Protocol.Definitions;

/// <summary>Agent 主定义，描述一个完整可运行的 Agent 配置。</summary>
public sealed class AgentKitDefinition
{
    /// <summary>定义唯一键。</summary>
    public required string Key { get; init; }

    /// <summary>显示名称。</summary>
    public required string Name { get; init; }

    /// <summary>描述信息。</summary>
    public string? Description { get; init; }

    /// <summary>版本号。</summary>
    public string? Version { get; init; }

    /// <summary>模型配置。</summary>
    public required ModelDefinition Model { get; init; }

    /// <summary>系统提示词。</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>执行策略。</summary>
    public ExecutionDefinition Execution { get; init; } = new();

    /// <summary>结构化输出定义。</summary>
    public StructuredOutputDefinition? StructuredOutput { get; init; }

    /// <summary>工具引用列表。</summary>
    public IReadOnlyList<ToolReference> Tools { get; init; } = [];

    /// <summary>技能集引用列表。</summary>
    public IReadOnlyList<SkillSetDefinition> Skills { get; init; } = [];

    /// <summary>MCP 服务引用列表。</summary>
    public IReadOnlyList<McpServerReference> McpServers { get; init; } = [];

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
