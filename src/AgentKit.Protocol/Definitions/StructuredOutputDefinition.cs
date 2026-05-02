using System.Text.Json;

namespace AgentKit.Protocol.Definitions;

/// <summary>结构化输出定义。</summary>
public sealed class StructuredOutputDefinition
{
    /// <summary>结构化输出名称标识。</summary>
    public required string Name { get; init; }

    /// <summary>版本号。</summary>
    public string? Version { get; init; }

    /// <summary>目标类型名称，用于 MAF 原生 RunAsync&lt;T&gt; 路径。</summary>
    public string? TargetTypeName { get; init; }

    /// <summary>JSON Schema 定义。</summary>
    public JsonDocument? Schema { get; init; }

    /// <summary>是否启用严格校验。</summary>
    public bool StrictValidation { get; init; } = true;

    /// <summary>是否启用自动修复（校验失败时发送修复请求给 AI）。</summary>
    public bool AutoRepair { get; init; }

    /// <summary>自定义修复提示词。为空时使用默认提示词。</summary>
    public string? RepairPrompt { get; init; }

    /// <summary>修复策略键。</summary>
    public string? RepairStrategyKey { get; init; }
}
