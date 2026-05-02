namespace AgentKit.Capabilities.Skills;

/// <summary>内存技能集注册表实现。</summary>
public sealed class InMemorySkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, SkillSetRegistration> _registrations = new(StringComparer.Ordinal);

    /// <summary>注册技能集。</summary>
    /// <param name="registration">技能集注册信息。</param>
    public void Register(SkillSetRegistration registration) => _registrations[registration.Key] = registration;

    /// <inheritdoc />
    public bool TryResolve(string key, out SkillSetRegistration? registration) => _registrations.TryGetValue(key, out registration);
}
