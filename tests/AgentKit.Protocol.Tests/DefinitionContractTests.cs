using System.Text.Json;

using AgentKit.Protocol.Definitions;
using AgentKit.Protocol.Enums;

using Xunit;

namespace AgentKit.Protocol.Tests;

/// <summary>定义协议契约测试。</summary>
public class DefinitionContractTests
{
    [Fact]
    public void AgentKitDefinition_RequiredProperties_Succeeds()
    {
        var definition = new AgentKitDefinition
        {
            Key = "test-agent",
            Name = "Test Agent",
            Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o" },
        };

        Assert.Equal("test-agent", definition.Key);
        Assert.Equal("Test Agent", definition.Name);
        Assert.Null(definition.Description);
        Assert.Null(definition.Version);
        Assert.Null(definition.SystemPrompt);
        Assert.NotNull(definition.Execution);
        Assert.Empty(definition.Tools);
        Assert.Empty(definition.Skills);
        Assert.Empty(definition.McpServers);
    }

    [Fact]
    public void AgentKitDefinition_AllProperties_Succeeds()
    {
        var definition = new AgentKitDefinition
        {
            Key = "full-agent",
            Name = "Full Agent",
            Description = "A complete agent",
            Version = "1.0.0",
            Model = new ModelDefinition { Provider = "azure", ModelId = "gpt-4o" },
            SystemPrompt = "You are helpful.",
            Execution = new ExecutionDefinition
            {
                StreamingEnabled = true,
                AllowMultipleToolCalls = true,
                PerServiceCallPersistence = true,
            },
            Tools = [new ToolReference { Key = "search" }],
            Skills = [new SkillSetDefinition { Key = "code-gen", Paths = ["/skills/code"] }],
            McpServers = [new McpServerReference { Key = "filesystem" }],
            Metadata = JsonDocument.Parse("{\"tag\": \"v1\"}"),
        };

        Assert.Equal("1.0.0", definition.Version);
        Assert.Equal("You are helpful.", definition.SystemPrompt);
        Assert.True(definition.Execution.StreamingEnabled);
        Assert.Single(definition.Tools);
        Assert.Single(definition.Skills);
        Assert.Single(definition.McpServers);
        Assert.NotNull(definition.Metadata);
    }

    [Fact]
    public void ModelDefinition_Defaults_Succeeds()
    {
        var model = new ModelDefinition
        {
            Provider = "openai",
            ModelId = "gpt-4o-mini",
        };

        Assert.True(model.SupportsFunctionTools);
        Assert.True(model.SupportsStructuredOutput);
        Assert.Null(model.Temperature);
        Assert.Null(model.TopP);
        Assert.Null(model.MaxOutputTokens);
    }

    [Fact]
    public void ModelDefinition_AllProperties_Succeeds()
    {
        var model = new ModelDefinition
        {
            Provider = "azure",
            ModelId = "gpt-4o",
            Endpoint = "https://example.openai.azure.com",
            CredentialKey = "azure-key",
            SupportsFunctionTools = true,
            SupportsStructuredOutput = false,
            Temperature = 0.7m,
            TopP = 0.9m,
            MaxOutputTokens = 4096,
        };

        Assert.Equal("azure", model.Provider);
        Assert.Equal("gpt-4o", model.ModelId);
        Assert.Equal("https://example.openai.azure.com", model.Endpoint);
        Assert.Equal("azure-key", model.CredentialKey);
        Assert.False(model.SupportsStructuredOutput);
        Assert.Equal(0.7m, model.Temperature);
        Assert.Equal(0.9m, model.TopP);
        Assert.Equal(4096, model.MaxOutputTokens);
    }

    [Fact]
    public void ExecutionDefinition_Defaults_Succeeds()
    {
        var execution = new ExecutionDefinition();

        Assert.True(execution.StreamingEnabled);
        Assert.False(execution.AllowMultipleToolCalls);
        Assert.Null(execution.AllowBackgroundResponses);
        Assert.False(execution.PerServiceCallPersistence);
        Assert.Null(execution.ModelSupportsToolsWithStructuredOutput);
        Assert.NotNull(execution.Retry);
        Assert.False(execution.Retry.Enabled);
    }

    [Fact]
    public void ToolReference_RequiresApproval_Succeeds()
    {
        var tool = new ToolReference
        {
            Key = "delete-file",
            Enabled = true,
            RequiresApproval = true,
            ApprovalReason = "Destructive operation",
        };

        Assert.Equal("delete-file", tool.Key);
        Assert.True(tool.RequiresApproval);
        Assert.Equal("Destructive operation", tool.ApprovalReason);
    }

    [Fact]
    public void SkillSetDefinition_Defaults_Succeeds()
    {
        var skill = new SkillSetDefinition
        {
            Key = "code-gen",
            Paths = ["/skills/code"],
        };

        Assert.Equal("code-gen", skill.Key);
        Assert.Single(skill.Paths);
        Assert.False(skill.ScriptApproval);
        Assert.False(skill.DisableCaching);
    }

    [Fact]
    public void McpServerReference_Defaults_Succeeds()
    {
        var mcp = new McpServerReference
        {
            Key = "filesystem",
        };

        Assert.Equal("filesystem", mcp.Key);
        Assert.True(mcp.Enabled);
        Assert.False(mcp.RequiresApproval);
    }

    [Fact]
    public void StructuredOutputDefinition_AutoRepair_Defaults_Succeeds()
    {
        var def = new StructuredOutputDefinition { Name = "Test" };

        Assert.False(def.AutoRepair);
        Assert.Null(def.RepairPrompt);
        Assert.True(def.StrictValidation);
    }

    [Fact]
    public void StructuredOutputDefinition_AutoRepair_Enabled_Succeeds()
    {
        var def = new StructuredOutputDefinition
        {
            Name = "Test",
            AutoRepair = true,
            RepairPrompt = "自定义修复提示词",
        };

        Assert.True(def.AutoRepair);
        Assert.Equal("自定义修复提示词", def.RepairPrompt);
    }
}
