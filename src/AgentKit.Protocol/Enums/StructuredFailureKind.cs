namespace AgentKit.Protocol.Enums;

/// <summary>结构化输出失败类型。</summary>
public enum StructuredFailureKind
{
    /// <summary>空输出。</summary>
    Empty,

    /// <summary>非法 JSON。</summary>
    InvalidJson,

    /// <summary>非法 Schema。</summary>
    InvalidSchema,

    /// <summary>Schema 不匹配。</summary>
    SchemaMismatch
}
