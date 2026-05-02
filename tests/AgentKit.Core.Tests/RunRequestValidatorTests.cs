using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Requests;

using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>运行请求校验器测试。</summary>
public class RunRequestValidatorTests
{
    [Fact]
    public void Validate_WithInput_ReturnsEmpty()
    {
        var request = new RunRequest { Input = "hello" };

        var errors = RunRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithMessages_ReturnsEmpty()
    {
        var request = new RunRequest
        {
            Messages = [new Message { Role = MessageRole.User, Text = "hello" }],
        };

        var errors = RunRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyInputAndMessages_ReturnsError()
    {
        var request = new RunRequest { Input = "", Messages = [] };

        var errors = RunRequestValidator.Validate(request);

        Assert.Single(errors);
        Assert.Contains(errors, e => e.Contains("不能同时为空"));
    }

    [Fact]
    public void Validate_NullInputNullMessages_ReturnsError()
    {
        var request = new RunRequest();

        var errors = RunRequestValidator.Validate(request);

        Assert.Single(errors);
    }

    [Fact]
    public void Validate_WhitespaceInput_ReturnsError()
    {
        var request = new RunRequest { Input = "   " };

        var errors = RunRequestValidator.Validate(request);

        Assert.Single(errors);
    }
}
