namespace AgentKit.Protocol.Enums;

/// <summary>失败类型。</summary>
public enum FailureKind
{
    /// <summary>未知错误。</summary>
    Unknown,

    /// <summary>模型调用失败。</summary>
    ModelCallFailed,

    /// <summary>工具执行失败。</summary>
    ToolExecutionFailed,

    /// <summary>结构化输出校验失败。</summary>
    StructuredOutputValidationFailed,

    /// <summary>审批超时。</summary>
    ApprovalTimeout,

    /// <summary>MCP Server 连接失败。</summary>
    McpConnectionFailed,

    /// <summary>Session 恢复失败。</summary>
    SessionRestoreFailed,

    /// <summary>Agent 定义解析失败。</summary>
    DefinitionResolutionFailed,

    /// <summary>配置无效。</summary>
    ConfigurationInvalid,

    /// <summary>内容过滤触发。</summary>
    ContentFilter,

    /// <summary>Token 超限。</summary>
    TokenLimitExceeded
}
