using System.Text.Json;

using Microsoft.Extensions.AI;

namespace AgentKit.Capabilities.Tools;

/// <summary>工具注册信息。</summary>
public sealed class ToolRegistration
{
    /// <summary>工具注册键。</summary>
    public required string Key { get; init; }

    /// <summary>显示名称。</summary>
    public string? DisplayName { get; init; }

    /// <summary>工具工厂，从服务提供者创建 AIFunction 实例。</summary>
    public required Func<IServiceProvider, AIFunction> Factory { get; init; }

    /// <summary>是否默认需要审批。</summary>
    public bool DefaultRequiresApproval { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
