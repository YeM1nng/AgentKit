using System.Text.Json;

using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Results;
using AgentKit.Protocol.Sessions;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Events;

/// <summary>将 MAF AgentResponse / AgentResponseUpdate 投影为 AgentKit 事件。</summary>
public sealed class EventProjector
{
    /// <summary>从流式更新投影事件。</summary>
    /// <param name="update">MAF 流式更新。</param>
    /// <param name="context">事件投影上下文。</param>
    /// <returns>投影的事件，若无可投影内容则返回 null。</returns>
    public KitEvent? Project(AgentResponseUpdate update, EventProjectionContext context)
    {
        if (!string.IsNullOrEmpty(update.Text))
        {
            return new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = context.RunId,
                SessionId = context.SessionId,
                Sequence = context.CurrentSequence,
                Attempt = context.Attempt,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                TextDelta = update.Text,
            };
        }

        if (update.Contents is { Count: > 0 })
        {
            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent fc)
                {
                    return CreateToolInvokedEvent(fc, context);
                }
            }
        }

        return null;
    }

    /// <summary>从非流式响应投影事件。</summary>
    /// <param name="response">MAF 非流式响应。</param>
    /// <param name="context">事件投影上下文。</param>
    /// <returns>投影的事件列表。</returns>
    public IReadOnlyList<KitEvent> Project(AgentResponse response, EventProjectionContext context)
    {
        var events = new List<KitEvent>();

        if (!string.IsNullOrEmpty(response.Text))
        {
            events.Add(new ResponseDeltaEvent
            {
                EventType = nameof(ResponseDeltaEvent),
                RunId = context.RunId,
                SessionId = context.SessionId,
                Sequence = context.CurrentSequence,
                Attempt = context.Attempt,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                TextDelta = response.Text,
            });
        }

        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent fc)
                {
                    events.Add(CreateToolInvokedEvent(fc, context));
                }
                else if (content is FunctionResultContent fr)
                {
                    var lastToolEvent = events.FindLast(e => e is ToolInvokedEvent) as ToolInvokedEvent;
                    if (lastToolEvent is not null)
                    {
                        events[^1] = lastToolEvent with
                        {
                            Result = fr.Result is null ? null
                                : JsonSerializer.SerializeToDocument(fr.Result.ToString() ?? string.Empty),
                            Succeeded = true,
                        };
                    }
                }
            }
        }

        return events;
    }

    private static ToolInvokedEvent CreateToolInvokedEvent(FunctionCallContent fc, EventProjectionContext context)
    {
        return new ToolInvokedEvent
        {
            EventType = nameof(ToolInvokedEvent),
            RunId = context.RunId,
            SessionId = context.SessionId,
            Sequence = context.CurrentSequence,
            Attempt = context.Attempt,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            CallId = fc.CallId ?? string.Empty,
            ToolName = fc.Name ?? string.Empty,
            ToolKind = ToolKind.Function,
            ProviderKey = "maf",
            Arguments = fc.Arguments is null ? null : JsonSerializer.SerializeToDocument(fc.Arguments),
        };
    }
}

/// <summary>事件投影上下文，携带投影所需的运行状态。</summary>
public sealed record EventProjectionContext(
    string RunId,
    string SessionId,
    long CurrentSequence,
    int Attempt);
