using System.Text.Json;

using AgentKit.Maf.Structured;
using AgentKit.Protocol.Enums;

using Xunit;

namespace AgentKit.Maf.Tests;

/// <summary>结构化输出校验器测试。</summary>
public class StructuredOutputValidatorTests
{
    private static JsonDocument ObjectSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "name": { "type": "string" },
            "age": { "type": "number" }
        },
        "required": ["name"]
    }
    """);

    [Fact]
    public void Validate_ValidJson_ReturnsValid()
    {
        var result = StructuredOutputValidator.Validate("{\"name\": \"test\", \"age\": 30}", ObjectSchema);

        Assert.True(result.IsValid);
        Assert.Equal("{\"name\": \"test\", \"age\": 30}", result.RawOutput);
    }

    [Fact]
    public void Validate_Empty_ReturnsEmptyFailure()
    {
        var result = StructuredOutputValidator.Validate("", ObjectSchema);

        Assert.False(result.IsValid);
        Assert.Equal(StructuredFailureKind.Empty, result.FailureKind);
    }

    [Fact]
    public void Validate_Whitespace_ReturnsEmptyFailure()
    {
        var result = StructuredOutputValidator.Validate("   ", ObjectSchema);

        Assert.False(result.IsValid);
        Assert.Equal(StructuredFailureKind.Empty, result.FailureKind);
    }

    [Fact]
    public void Validate_InvalidJson_ReturnsInvalidJsonFailure()
    {
        var result = StructuredOutputValidator.Validate("{invalid", ObjectSchema);

        Assert.False(result.IsValid);
        Assert.Equal(StructuredFailureKind.InvalidJson, result.FailureKind);
    }

    [Fact]
    public void Validate_MissingRequired_ReturnsSchemaMismatch()
    {
        var result = StructuredOutputValidator.Validate("{\"age\": 30}", ObjectSchema);

        Assert.False(result.IsValid);
        Assert.Equal(StructuredFailureKind.SchemaMismatch, result.FailureKind);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("name"));
    }

    [Fact]
    public void Validate_WrongType_ReturnsSchemaMismatch()
    {
        var schema = JsonDocument.Parse("""{"type": "object"}""");
        var result = StructuredOutputValidator.Validate("\"just a string\"", schema);

        Assert.False(result.IsValid);
        Assert.Equal(StructuredFailureKind.SchemaMismatch, result.FailureKind);
    }

    [Fact]
    public void Validate_CorrectType_ReturnsValid()
    {
        var schema = JsonDocument.Parse("""{"type": "string"}""");
        var result = StructuredOutputValidator.Validate("\"hello\"", schema);

        Assert.True(result.IsValid);
    }
}
