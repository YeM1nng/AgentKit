using AgentKit.Protocol.Definitions;

using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>定义校验器测试。</summary>
public class DefinitionValidatorTests
{
    [Fact]
    public void Validate_ValidDefinition_ReturnsEmpty()
    {
        var definition = new AgentKitDefinition
        {
            Key = "test",
            Name = "Test",
            Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o" },
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyKey_ReturnsError()
    {
        var definition = new AgentKitDefinition
        {
            Key = "",
            Name = "Test",
            Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o" },
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Contains(errors, e => e.Contains("定义键"));
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var definition = new AgentKitDefinition
        {
            Key = "test",
            Name = "",
            Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o" },
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Contains(errors, e => e.Contains("显示名称"));
    }

    [Fact]
    public void Validate_NullModel_ReturnsError()
    {
        var definition = new AgentKitDefinition
        {
            Key = "test",
            Name = "Test",
            Model = null!,
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Contains(errors, e => e.Contains("模型配置"));
    }

    [Fact]
    public void Validate_EmptyProvider_ReturnsError()
    {
        var definition = new AgentKitDefinition
        {
            Key = "test",
            Name = "Test",
            Model = new ModelDefinition { Provider = "", ModelId = "gpt-4o" },
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Contains(errors, e => e.Contains("提供商标识"));
    }

    [Fact]
    public void Validate_EmptyModelId_ReturnsError()
    {
        var definition = new AgentKitDefinition
        {
            Key = "test",
            Name = "Test",
            Model = new ModelDefinition { Provider = "openai", ModelId = "" },
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Contains(errors, e => e.Contains("模型 ID"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAll()
    {
        var definition = new AgentKitDefinition
        {
            Key = "",
            Name = "",
            Model = new ModelDefinition { Provider = "", ModelId = "" },
        };

        var errors = DefinitionValidator.Validate(definition);

        Assert.Equal(4, errors.Count);
    }
}
