namespace AgentKit.Capabilities.Tools;

/// <summary>内存工具注册表实现。</summary>
public sealed class InMemoryToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ToolRegistration> _registrations = new(StringComparer.Ordinal);

    /// <summary>注册工具。</summary>
    /// <param name="registration">工具注册信息。</param>
    public void Register(ToolRegistration registration) => _registrations[registration.Key] = registration;

    /// <inheritdoc />
    public bool TryResolve(string key, out ToolRegistration? registration) => _registrations.TryGetValue(key, out registration);
}
