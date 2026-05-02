using AgentKit.Capabilities.Skills;

using Microsoft.Agents.AI;

namespace AgentKit.Maf.Skills;

/// <summary>从技能集注册信息构建 MAF AgentSkillsProvider。</summary>
public static class SkillProviderFactory
{
    /// <summary>从注册的技能集创建 AgentSkillsProvider。</summary>
    /// <param name="registrations">技能集注册列表。</param>
    /// <returns>AgentSkillsProvider 实例，无技能时返回 null。</returns>
    public static AgentSkillsProvider? Create(IReadOnlyList<SkillSetRegistration> registrations)
    {
        if (registrations.Count == 0)
            return null;

        var builder = new AgentSkillsProviderBuilder();

        foreach (var registration in registrations)
        {
            if (registration.Paths.Count > 0)
            {
                builder.UseFileSkills(registration.Paths);
            }

            if (registration.ScriptApproval)
                builder.UseScriptApproval();

            if (registration.PromptTemplate is not null)
                builder.UsePromptTemplate(registration.PromptTemplate);
        }

        return builder.Build();
    }
}
