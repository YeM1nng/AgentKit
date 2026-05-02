namespace AgentKit.Storage;

/// <summary>审批目标类型。</summary>
public enum ApprovalTargetKind
{
    /// <summary>函数工具调用。</summary>
    FunctionTool,

    /// <summary>MCP 工具调用。</summary>
    McpTool,

    /// <summary>Skill 脚本执行。</summary>
    SkillScript
}
