using System.Text.Json;
using System.Threading;

using AgentKit.Storage;
using AgentKit.Storage.Models;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Sessions;

/// <summary>桥接 ISessionStore 与 MAF ChatHistoryProvider。</summary>
/// <remarks>
/// 从 ISessionStore 加载/存储消息历史，通过 SessionId 关联会话。
/// 消息以 JSON 格式存储，保留完整的 MAF ChatMessage 结构。
/// </remarks>
public sealed class ChatHistoryProviderBridge : ChatHistoryProvider
{
    private readonly ISessionStore _sessionStore;
    private readonly string _sessionId;

    /// <summary>构造函数。</summary>
    /// <param name="sessionStore">会话存储。</param>
    /// <param name="sessionId">会话 ID。</param>
    public ChatHistoryProviderBridge(ISessionStore sessionStore, string sessionId)
    {
        _sessionStore = sessionStore;
        _sessionId = sessionId;
    }

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context, CancellationToken cancellationToken)
    {
        var storedMessages = await _sessionStore.LoadMessagesAsync(_sessionId, cancellationToken);
        return storedMessages
            .Select(ToChatMessage)
            .Where(m => m is not null)!;
    }

    /// <inheritdoc />
    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context, CancellationToken cancellationToken)
    {
        var allMessages = (context.RequestMessages ?? []).Concat(context.ResponseMessages ?? []);
        var storedMessages = allMessages
            .Where(m => !ContainsToolApprovalContent(m))
            .Select((m, i) => ToStoredMessage(m, i, _sessionId))
            .ToList();

        if (storedMessages.Count > 0)
        {
            await _sessionStore.AppendMessagesAsync(_sessionId, storedMessages, cancellationToken);
        }
    }

    private static ChatMessage? ToChatMessage(StoredMessage stored)
    {
        var role = MapRole(stored.Role);
        var text = ExtractText(stored.Contents);
        return new ChatMessage(role, text ?? string.Empty);
    }

    private static long _sequenceCounter = DateTimeOffset.UtcNow.Ticks;

    private static StoredMessage ToStoredMessage(ChatMessage message, int index, string sessionId)
    {
        return new StoredMessage
        {
            SessionId = sessionId,
            Sequence = Interlocked.Increment(ref _sequenceCounter),
            Role = MapChatRole(message.Role),
            Contents = JsonSerializer.SerializeToElement(new { text = message.Text ?? string.Empty }),
            CreatedAtUtc = message.CreatedAt ?? DateTimeOffset.UtcNow,
        };
    }

    private static string? ExtractText(JsonElement? contents)
    {
        if (contents is null) return null;
        if (contents.Value.TryGetProperty("text", out var textProp))
            return textProp.GetString();
        return null;
    }

    private static ChatRole MapRole(MessageRole role) => role switch
    {
        MessageRole.System => ChatRole.System,
        MessageRole.User => ChatRole.User,
        MessageRole.Assistant => ChatRole.Assistant,
        MessageRole.Tool => ChatRole.Tool,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "未知的消息角色。"),
    };

    private static MessageRole MapChatRole(ChatRole role)
    {
        if (role == ChatRole.System) return MessageRole.System;
        if (role == ChatRole.Assistant) return MessageRole.Assistant;
        if (role == ChatRole.Tool) return MessageRole.Tool;
        return MessageRole.User;
    }

    private static bool ContainsToolApprovalContent(ChatMessage message)
    {
        return message.Contents?.Any(c =>
            c is ToolApprovalRequestContent or ToolApprovalResponseContent) ?? false;
    }
}
