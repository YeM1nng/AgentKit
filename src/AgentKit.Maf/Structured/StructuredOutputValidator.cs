using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Results;

namespace AgentKit.Maf.Structured;

/// <summary>对模型输出执行 JSON Schema 校验。</summary>
public static class StructuredOutputValidator
{
    /// <summary>校验输出文本是否符合 JSON Schema。</summary>
    public static StructuredValidationResult Validate(string output, JsonDocument schema)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new StructuredValidationResult
            {
                IsValid = false,
                FailureKind = StructuredFailureKind.Empty,
                Errors = ["输出为空"],
                RawOutput = output,
            };
        }

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(output);
        }
        catch (JsonException ex)
        {
            return new StructuredValidationResult
            {
                IsValid = false,
                FailureKind = StructuredFailureKind.InvalidJson,
                Errors = [$"JSON 解析失败: {ex.Message}"],
                RawOutput = output,
            };
        }

        using (parsed)
        {
            // 基础校验：验证为合法 JSON 且结构匹配 schema 的 type 字段
            var errors = ValidateAgainstSchema(parsed.RootElement, schema.RootElement);
            if (errors.Count > 0)
            {
                return new StructuredValidationResult
                {
                    IsValid = false,
                    FailureKind = StructuredFailureKind.SchemaMismatch,
                    Errors = errors,
                    RawOutput = output,
                };
            }

            return new StructuredValidationResult
            {
                IsValid = true,
                RawOutput = output,
            };
        }
    }

    private static List<string> ValidateAgainstSchema(JsonElement value, JsonElement schema)
    {
        var errors = new List<string>();

        if (schema.TryGetProperty("type", out var typeElement))
        {
            var expectedType = typeElement.GetString();
            if (!MatchesType(value, expectedType))
            {
                errors.Add($"期望类型 '{expectedType}'，实际 '{value.ValueKind}'");
            }
        }

        if (schema.TryGetProperty("properties", out var properties)
            && value.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out var required))
            {
                foreach (var req in required.EnumerateArray())
                {
                    var name = req.GetString();
                    if (name != null && !value.TryGetProperty(name, out _))
                    {
                        errors.Add($"缺少必需属性 '{name}'");
                    }
                }
            }
        }

        return errors;
    }

    private static bool MatchesType(JsonElement value, string? expectedType) => (expectedType, value.ValueKind) switch
    {
        ("object", JsonValueKind.Object) => true,
        ("array", JsonValueKind.Array) => true,
        ("string", JsonValueKind.String) => true,
        ("number", JsonValueKind.Number) => true,
        ("integer", JsonValueKind.Number) => true,
        ("boolean", JsonValueKind.True or JsonValueKind.False) => true,
        ("null", JsonValueKind.Null) => true,
        _ => false,
    };
}
