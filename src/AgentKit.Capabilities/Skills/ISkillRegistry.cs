namespace AgentKit.Capabilities.Skills;

/// <summary>技能集注册表，通过 Key 解析技能集注册信息。</summary>
public interface ISkillRegistry
{
    /// <summary>尝试解析技能集注册信息。</summary>
    /// <param name="key">技能集唯一键。</param>
    /// <param name="registration">技能集注册信息，未找到时为 null。</param>
    /// <returns>是否找到。</returns>
    bool TryResolve(string key, out SkillSetRegistration? registration);
}
