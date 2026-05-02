using System.Text.Json;

namespace AgentKit.Protocol.Definitions;

/// <summary>模型配置定义。</summary>
public sealed class ModelDefinition
{
    /// <summary>模型提供商标识。</summary>
    public required string Provider { get; init; }

    /// <summary>模型 ID。</summary>
    public required string ModelId { get; init; }

    /// <summary>API 端点。</summary>
    public string? Endpoint { get; init; }

    /// <summary>凭证密钥标识。</summary>
    public string? CredentialKey { get; init; }

    /// <summary>是否支持函数工具调用。</summary>
    public bool SupportsFunctionTools { get; init; } = true;

    /// <summary>是否支持结构化输出。</summary>
    public bool SupportsStructuredOutput { get; init; } = true;

    /// <summary>温度参数。</summary>
    public decimal? Temperature { get; init; }

    /// <summary>Top-P 参数。</summary>
    public decimal? TopP { get; init; }

    /// <summary>最大输出 Token 数。</summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>自定义元数据。</summary>
    public JsonDocument? Metadata { get; init; }
}
