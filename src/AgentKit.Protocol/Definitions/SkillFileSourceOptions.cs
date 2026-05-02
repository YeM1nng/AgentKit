namespace AgentKit.Protocol.Definitions;

/// <summary>技能文件发现选项。</summary>
public sealed class SkillFileSourceOptions
{
    /// <summary>是否递归搜索子目录。</summary>
    public bool RecurseSubdirectories { get; init; }

    /// <summary>包含的文件模式列表。</summary>
    public IReadOnlyList<string>? IncludePatterns { get; init; }

    /// <summary>排除的文件模式列表。</summary>
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
}
