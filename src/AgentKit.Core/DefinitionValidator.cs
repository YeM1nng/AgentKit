using AgentKit.Protocol.Definitions;

namespace AgentKit.Core;

/// <summary>校验 AgentKitDefinition 的完整性与合法性。</summary>
public static class DefinitionValidator
{
    /// <summary>校验定义，返回校验错误列表。空列表表示通过。</summary>
    /// <param name="definition">Agent 定义。</param>
    /// <returns>校验错误列表。</returns>
    public static IReadOnlyList<string> Validate(AgentKitDefinition definition)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(definition.Key))
            errors.Add("定义键不能为空。");

        if (string.IsNullOrWhiteSpace(definition.Name))
            errors.Add("显示名称不能为空。");

        if (definition.Model is null)
            errors.Add("模型配置不能为空。");
        else
            ValidateModel(definition.Model, errors);

        return errors;
    }

    private static void ValidateModel(ModelDefinition model, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(model.Provider))
            errors.Add("模型提供商标识不能为空。");

        if (string.IsNullOrWhiteSpace(model.ModelId))
            errors.Add("模型 ID 不能为空。");
    }
}
