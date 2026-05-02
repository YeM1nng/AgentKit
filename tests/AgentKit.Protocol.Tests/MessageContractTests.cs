using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Requests;

using Xunit;

namespace AgentKit.Protocol.Tests;

/// <summary>消息协议契约测试。</summary>
public class MessageContractTests
{
    [Fact]
    public void Message_TextOnly_Succeeds()
    {
        var msg = new Message
        {
            Role = MessageRole.User,
            Text = "Hello",
        };

        Assert.Equal(MessageRole.User, msg.Role);
        Assert.Equal("Hello", msg.Text);
        Assert.Null(msg.Contents);
    }

    [Fact]
    public void Message_WithAgentKitTextContent_Succeeds()
    {
        var msg = new Message
        {
            Role = MessageRole.Assistant,
            Contents = [new AgentKitTextContent { ContentType = "text", Text = "Response" }],
        };

        Assert.Single(msg.Contents!);
        var content = (AgentKitTextContent)msg.Contents[0];
        Assert.Equal("Response", content.Text);
    }

    [Fact]
    public void Message_WithFunctionCall_Succeeds()
    {
        var args = JsonDocument.Parse("{\"query\": \"test\"}");
        var msg = new Message
        {
            Role = MessageRole.Assistant,
            Contents =
            [
                new AgentKitFunctionCallContent
                {
                    ContentType = "function_call",
                    CallId = "call-1",
                    Name = "search",
                    Arguments = args,
                },
            ],
        };

        var fc = (AgentKitFunctionCallContent)msg.Contents![0];
        Assert.Equal("call-1", fc.CallId);
        Assert.Equal("search", fc.Name);
        Assert.Equal("test", fc.Arguments.RootElement.GetProperty("query").GetString());
    }

    [Fact]
    public void Message_WithFunctionResult_Succeeds()
    {
        var result = JsonDocument.Parse("{\"results\": [1, 2, 3]}");
        var msg = new Message
        {
            Role = MessageRole.User,
            Contents =
            [
                new AgentKitFunctionResultContent
                {
                    ContentType = "function_result",
                    CallId = "call-1",
                    Name = "search",
                    Result = result,
                },
            ],
        };

        var fr = (AgentKitFunctionResultContent)msg.Contents![0];
        Assert.Equal("call-1", fr.CallId);
        Assert.Equal("search", fr.Name);
        Assert.False(fr.IsError);
    }

    [Fact]
    public void MessageRole_AllMembers_Defined()
    {
        Assert.Equal(0, (int)MessageRole.System);
        Assert.Equal(1, (int)MessageRole.User);
        Assert.Equal(2, (int)MessageRole.Assistant);
        Assert.Equal(3, (int)MessageRole.Tool);
    }
}
