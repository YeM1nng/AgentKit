using System.Text.Json;

using AgentKit.Maf.Structured;
using AgentKit.Protocol.Definitions;

using Microsoft.Extensions.AI;

using Xunit;

namespace AgentKit.Maf.Tests;

/// <summary>结构化输出协调器测试。</summary>
public class StructuredOutputCoordinatorTests
{
    [Fact]
    public void Configure_NullDefinition_DoesNotSetFormat()
    {
        var options = new ChatOptions();
        StructuredOutputCoordinator.Configure(options, null);

        Assert.Null(options.ResponseFormat);
    }

    [Fact]
    public void Configure_WithSchema_SetsJsonFormat()
    {
        var schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": { "name": { "type": "string" } }
        }
        """);

        var definition = new StructuredOutputDefinition
        {
            Name = "Person",
            Version = "1.0",
            Schema = schema,
        };

        var options = new ChatOptions();
        StructuredOutputCoordinator.Configure(options, definition);

        Assert.NotNull(options.ResponseFormat);
    }

    [Fact]
    public void Configure_NullSchema_DoesNotSetFormat()
    {
        var definition = new StructuredOutputDefinition
        {
            Name = "Person",
            Schema = null,
        };

        var options = new ChatOptions();
        StructuredOutputCoordinator.Configure(options, definition);

        Assert.Null(options.ResponseFormat);
    }

    [Fact]
    public void RequiresTwoPhaseExecution_NoDefinition_ReturnsFalse()
    {
        var execution = new ExecutionDefinition();
        Assert.False(StructuredOutputCoordinator.RequiresTwoPhaseExecution(null, execution, true));
    }

    [Fact]
    public void RequiresTwoPhaseExecution_NoTools_ReturnsFalse()
    {
        var definition = new StructuredOutputDefinition { Name = "Test" };
        var execution = new ExecutionDefinition();

        Assert.False(StructuredOutputCoordinator.RequiresTwoPhaseExecution(definition, execution, hasTools: false));
    }

    [Fact]
    public void RequiresTwoPhaseExecution_ToolsAndSupports_ReturnsFalse()
    {
        var definition = new StructuredOutputDefinition { Name = "Test" };
        var execution = new ExecutionDefinition { ModelSupportsToolsWithStructuredOutput = true };

        Assert.False(StructuredOutputCoordinator.RequiresTwoPhaseExecution(definition, execution, hasTools: true));
    }

    [Fact]
    public void RequiresTwoPhaseExecution_ToolsAndNoSupport_ReturnsTrue()
    {
        var definition = new StructuredOutputDefinition { Name = "Test" };
        var execution = new ExecutionDefinition();

        Assert.True(StructuredOutputCoordinator.RequiresTwoPhaseExecution(definition, execution, hasTools: true));
    }
}
