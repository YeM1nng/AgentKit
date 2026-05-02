using System.Runtime.CompilerServices;
using System.Text.Json;

using AgentKit.Abstractions;
using AgentKit.Core;
using AgentKit.Maf.Approvals;
using AgentKit.Maf.Composition;
using System.Text;
using AgentKit.Maf.Events;
using AgentKit.Maf.Sessions;
using AgentKit.Maf.Structured;
using AgentKit.Protocol.Definitions;
using AgentKit.Protocol.Enums;
using AgentKit.Protocol.Events;
using AgentKit.Protocol.Requests;
using AgentKit.Protocol.Results;
using AgentKit.Protocol.Sessions;
using AgentKit.Storage;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Execution;

/// <summary>基于 MAF 的执行管道实现。</summary>
public sealed class MafExecutionPipeline : IExecutionPipeline
{
    private readonly Composer _composer;
    private readonly SessionAdapter _sessionAdapter;
    private readonly EventProjector _eventProjector;
    private readonly IModelClientResolver _modelClientResolver;
    private readonly ISessionStore _sessionStore;
    private readonly IApprovalStore? _approvalStore;
    private readonly IEventStore? _eventStore;
    private readonly IStructuredTypeResolver? _structuredTypeResolver;
    private readonly IStructuredOutputRepairStrategy? _repairStrategy;

    /// <summary>构造函数。</summary>
    /// <param name="composer">MAF Agent 组装器。</param>
    /// <param name="sessionAdapter">会话适配器。</param>
    /// <param name="eventProjector">事件投影器。</param>
    /// <param name="modelClientResolver">模型客户端解析器。</param>
    /// <param name="sessionStore">会话存储。</param>
    /// <param name="approvalStore">审批存储，可选。</param>
    /// <param name="eventStore">事件存储，可选。</param>
    /// <param name="structuredTypeResolver">结构化类型解析器，可选。</param>
    /// <param name="repairStrategy">结构化输出修复策略，可选。</param>
    public MafExecutionPipeline(
        Composer composer,
        SessionAdapter sessionAdapter,
        EventProjector eventProjector,
        IModelClientResolver modelClientResolver,
        ISessionStore sessionStore,
        IApprovalStore? approvalStore = null,
        IEventStore? eventStore = null,
        IStructuredTypeResolver? structuredTypeResolver = null,
        IStructuredOutputRepairStrategy? repairStrategy = null)
    {
        _composer = composer;
        _sessionAdapter = sessionAdapter;
        _eventProjector = eventProjector;
        _modelClientResolver = modelClientResolver;
        _sessionStore = sessionStore;
        _approvalStore = approvalStore;
        _eventStore = eventStore;
        _structuredTypeResolver = structuredTypeResolver;
        _repairStrategy = repairStrategy;
    }

    /// <inheritdoc />
    public async Task<PipelineResult> RunAsync(
        AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken)
    {
        var runId = SessionCoordinator.CreateRunId();
        var sessionId = SessionCoordinator.ResolveSessionId(request);

        var chatClient = await _modelClientResolver.ResolveAsync(definition.Model, cancellationToken);

        ChatHistoryProvider? historyProvider = null;
        if (request.Session?.SessionId is not null)
        {
            historyProvider = new ChatHistoryProviderBridge(_sessionStore, sessionId);
        }

        var composed = _composer.Compose(definition, chatClient, historyProvider);

        var session = request.Session?.AgentSessionData is { } sessionData
            ? await _sessionAdapter.RestoreAsync(composed.Agent, sessionData, cancellationToken)
            : await _sessionAdapter.CreateAsync(composed.Agent, cancellationToken);

        var messages = BuildMessages(request).ToList();

        // 审批恢复：构造恢复消息并 prepend
        if (request.ApprovalDecision is not null
            && request.Session?.ResumptionContext.PendingApprovals is { Count: > 0 } pendingApprovals)
        {
            messages.Insert(0, ApprovalResumeFactory.CreateResumeMessage(
                pendingApprovals[0], request.ApprovalDecision!));
        }

        // 构建运行选项（续跑令牌）
        AgentRunOptions? runOptions = null;
        if (request.ContinuationToken is not null)
        {
            runOptions = new AgentRunOptions
            {
                ContinuationToken = ResponseContinuationToken.FromBytes(Encoding.UTF8.GetBytes(request.ContinuationToken)),
            };
        }

        // 发射运行开始事件
        var startEvent = CreateRunStartedEvent(runId, sessionId, 0, 1);
        await PersistEventAsync(startEvent, cancellationToken);

        // 重试循环
        var retryPolicy = definition.Execution.Retry;
        var maxAttempts = retryPolicy.Enabled ? retryPolicy.MaxAttempts : 1;
        int attempt;
        AgentResponse? response = null;
        IReadOnlyList<KitEvent> events = [];
        long sequence = 1; // 0 留给 RunStartedEvent

        for (attempt = 1; attempt <= maxAttempts; attempt++)
        {
            response = await composed.Agent.RunAsync(
                messages, session, runOptions, cancellationToken);

            var context = new EventProjectionContext(
                RunId: runId,
                SessionId: sessionId,
                CurrentSequence: sequence,
                Attempt: attempt);

            var rawEvents = _eventProjector.Project(response, context);

            // 为每个事件分配递增序号
            var sequencedEvents = new List<KitEvent>(rawEvents.Count);
            foreach (var ev in rawEvents)
            {
                sequencedEvents.Add(ev with { Sequence = sequence++ });
            }
            events = sequencedEvents;

            // 派发事件到存储
            foreach (var ev in events)
                await PersistEventAsync(ev, cancellationToken);

            // 检测运行失败
            var hasFailure = events.Any(e => e is RunFailedEvent);
            if (hasFailure)
            {
                var hasVisibleOutput = events.Any(e => e is ResponseDeltaEvent { TextDelta.Length: > 0 });

                if (RetryCoordinator.ShouldRetry(retryPolicy, attempt, hasVisibleOutput))
                {
                    var delay = RetryCoordinator.CalculateDelay(retryPolicy.BaseDelay, attempt);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                break;
            }

            // 结构化输出校验 + 修复
            if (definition.StructuredOutput?.Schema is { } schema
                && response.Text is not null)
            {
                var validation = StructuredOutputValidator.Validate(response.Text, schema);

                if (!validation.IsValid)
                {
                    // 尝试自定义修复策略
                    if (_repairStrategy is not null
                        && definition.StructuredOutput.RepairStrategyKey is not null)
                    {
                        var repaired = await _repairStrategy.TryRepairAsync(
                            response.Text, validation, cancellationToken);

                        if (repaired is not null)
                        {
                            var revalidation = StructuredOutputValidator.Validate(repaired, schema);
                            if (revalidation.IsValid)
                            {
                                var payload = DeserializeStructuredPayload(
                                    repaired, definition.StructuredOutput);
                                return await BuildSuccessResult(
                                    composed.Agent, session, sessionId, definition,
                                    repaired, events, revalidation, payload, runId, sequence, attempt, cancellationToken);
                            }
                        }
                    }

                    // 自动修复：调用 AI 修正格式错误
                    if (definition.StructuredOutput.AutoRepair)
                    {
                        var repaired = await AutoRepairAsync(
                            chatClient, response.Text, validation, schema,
                            definition.StructuredOutput.RepairPrompt, cancellationToken);

                        if (repaired is not null)
                        {
                            var revalidation = StructuredOutputValidator.Validate(repaired, schema);
                            if (revalidation.IsValid)
                            {
                                var payload = DeserializeStructuredPayload(
                                    repaired, definition.StructuredOutput);
                                return await BuildSuccessResult(
                                    composed.Agent, session, sessionId, definition,
                                    repaired, events, revalidation, payload, runId, sequence, attempt, cancellationToken);
                            }
                        }
                    }

                    // 修复失败，检查重试
                    var hasVisibleOutput = !string.IsNullOrWhiteSpace(response.Text);
                    if (retryPolicy.RetryStructuredFinalization
                        && RetryCoordinator.ShouldRetry(retryPolicy, attempt, hasVisibleOutput))
                    {
                        var delay = RetryCoordinator.CalculateDelay(retryPolicy.BaseDelay, attempt);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    var failSession = await BuildSessionState(composed.Agent, session, sessionId, definition, cancellationToken);
                    return new PipelineResult(response.Text, failSession, events, validation, null, null, attempt);
                }

                // 校验通过
                var payload2 = DeserializeStructuredPayload(response.Text, definition.StructuredOutput);
                return await BuildSuccessResult(
                    composed.Agent, session, sessionId, definition,
                    response.Text, events, validation, payload2, runId, sequence, attempt, cancellationToken);
            }

            break; // 成功且无结构化输出
        }

        // 提取续跑令牌
        string? continuationToken = null;
        if (response?.ContinuationToken is not null)
        {
            continuationToken = Encoding.UTF8.GetString(response.ContinuationToken.ToBytes().Span);
        }

        var finalSessionState = await BuildSessionState(
            composed.Agent, session, sessionId, definition, cancellationToken);

        // 创建审批单
        var pendingApproval = ApprovalCoordinator.ExtractPendingApproval(events);
        if (pendingApproval is not null && _approvalStore is not null)
        {
            var storedApproval = new Storage.Models.StoredApproval
            {
                RequestId = pendingApproval.RequestId,
                RunId = runId,
                SessionId = sessionId,
                TargetKind = (Storage.ApprovalTargetKind)(int)pendingApproval.TargetKind,
                TargetName = pendingApproval.TargetName,
                Arguments = pendingApproval.Arguments,
                Reason = pendingApproval.Reason,
                AssistantText = pendingApproval.AssistantText,
            };
            await _approvalStore.CreateAsync(storedApproval, cancellationToken);
        }

        // 发射运行完成事件
        var completedEvent = CreateRunCompletedEvent(runId, sessionId, sequence, attempt - 1);
        await PersistEventAsync(completedEvent, cancellationToken);

        return new PipelineResult(response?.Text, finalSessionState, events, null, null, continuationToken, attempt - 1);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<KitEvent> StreamAsync(
        AgentKitDefinition definition, RunRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var runId = SessionCoordinator.CreateRunId();
        var sessionId = SessionCoordinator.ResolveSessionId(request);

        var chatClient = await _modelClientResolver.ResolveAsync(definition.Model, cancellationToken);

        ChatHistoryProvider? historyProvider = null;
        if (request.Session?.SessionId is not null)
        {
            historyProvider = new ChatHistoryProviderBridge(_sessionStore, sessionId);
        }

        var composed = _composer.Compose(definition, chatClient, historyProvider);

        var session = request.Session?.AgentSessionData is { } sessionData
            ? await _sessionAdapter.RestoreAsync(composed.Agent, sessionData, cancellationToken)
            : await _sessionAdapter.CreateAsync(composed.Agent, cancellationToken);

        var messages = BuildMessages(request);

        // 审批恢复
        if (request.ApprovalDecision is not null
            && request.Session?.ResumptionContext.PendingApprovals is { Count: > 0 } pendingApprovals)
        {
            var list = messages.ToList();
            list.Insert(0, ApprovalResumeFactory.CreateResumeMessage(
                pendingApprovals[0], request.ApprovalDecision!));
            messages = list;
        }

        // 发射运行开始事件
        var startEvent = CreateRunStartedEvent(runId, sessionId, 0, 1);
        await PersistEventAsync(startEvent, cancellationToken);
        yield return startEvent;

        var context = new EventProjectionContext(
            RunId: runId,
            SessionId: sessionId,
            CurrentSequence: 0,
            Attempt: 1);

        long sequence = 1; // 从 1 开始，0 留给 RunStartedEvent
        await foreach (var update in composed.Agent.RunStreamingAsync(messages, session, cancellationToken: cancellationToken))
        {
            var ev = _eventProjector.Project(update, context with { CurrentSequence = sequence++ });
            if (ev is not null)
            {
                await PersistEventAsync(ev, cancellationToken);
                yield return ev;
            }
        }

        // 发射运行完成事件
        var completedEvent = CreateRunCompletedEvent(runId, sessionId, sequence, 1);
        await PersistEventAsync(completedEvent, cancellationToken);
        yield return completedEvent;

        // 流式完成后持久化会话
        await BuildSessionState(composed.Agent, session, sessionId, definition, cancellationToken);
    }

    private async Task<PipelineResult> BuildSuccessResult(
        AIAgent agent, AgentSession session, string sessionId,
        AgentKitDefinition definition, string text,
        IReadOnlyList<KitEvent> events, StructuredValidationResult validation,
        JsonDocument? payload, string runId, long sequence, int attempt, CancellationToken cancellationToken)
    {
        var sessionState = await BuildSessionState(agent, session, sessionId, definition, cancellationToken);

        // 发射运行完成事件
        var completedEvent = CreateRunCompletedEvent(runId, sessionId, sequence, attempt);
        await PersistEventAsync(completedEvent, cancellationToken);

        return new PipelineResult(text, sessionState, events, validation, payload, null, attempt);
    }

    private async Task<SessionState> BuildSessionState(
        AIAgent agent, AgentSession session, string sessionId,
        AgentKitDefinition definition, CancellationToken cancellationToken)
    {
        var serialized = await _sessionAdapter.SerializeAsync(agent, session, cancellationToken);

        var storedSession = new Storage.Models.StoredSession
        {
            SessionId = sessionId,
            DefinitionKey = definition.Key,
            DefinitionVersion = definition.Version,
            AgentSessionData = serialized,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        await _sessionStore.SaveAsync(storedSession, cancellationToken);

        return new SessionState
        {
            SessionId = sessionId,
            AgentSessionData = serialized,
        };
    }

    private async Task PersistEventAsync(KitEvent @event, CancellationToken cancellationToken)
    {
        if (_eventStore is not null)
        {
            await _eventStore.AppendAsync(ToStoredEvent(@event), cancellationToken);
        }
    }

    private JsonDocument? DeserializeStructuredPayload(string text, StructuredOutputDefinition definition)
    {
        if (definition.TargetTypeName is not null && _structuredTypeResolver is not null)
        {
            if (_structuredTypeResolver.TryResolve(definition.TargetTypeName, out var type) && type is not null)
            {
                var obj = JsonSerializer.Deserialize(text, type);
                if (obj is not null)
                    return JsonSerializer.SerializeToDocument(obj);
            }
        }

        return JsonDocument.Parse(text);
    }

    private static RunStartedEvent CreateRunStartedEvent(string runId, string sessionId, long sequence, int attempt)
    {
        return new RunStartedEvent
        {
            EventType = nameof(RunStartedEvent),
            RunId = runId,
            SessionId = sessionId,
            Sequence = sequence,
            Attempt = attempt,
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static RunCompletedEvent CreateRunCompletedEvent(string runId, string sessionId, long sequence, int attempt)
    {
        return new RunCompletedEvent
        {
            EventType = nameof(RunCompletedEvent),
            RunId = runId,
            SessionId = sessionId,
            Sequence = sequence,
            Attempt = attempt,
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static Storage.Models.StoredEvent ToStoredEvent(KitEvent @event)
    {
        return new Storage.Models.StoredEvent
        {
            RunId = @event.RunId,
            SessionId = @event.SessionId,
            Sequence = @event.Sequence,
            EventType = @event.EventType,
            Payload = JsonSerializer.SerializeToDocument(@event),
            Attempt = @event.Attempt,
            OccurredAtUtc = @event.OccurredAtUtc,
        };
    }

    private static IEnumerable<ChatMessage> BuildMessages(RunRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Input))
            yield return new ChatMessage(ChatRole.User, request.Input);

        if (request.Messages is not null)
        {
            foreach (var msg in request.Messages)
            {
                var role = msg.Role switch
                {
                    Protocol.Enums.MessageRole.System => ChatRole.System,
                    Protocol.Enums.MessageRole.Assistant => ChatRole.Assistant,
                    _ => ChatRole.User,
                };

                if (msg.Contents is { Count: > 0 })
                {
                    var meaiContents = msg.Contents
                        .Select(c => ConvertContent(c))
                        .Where(c => c is not null)
                        .ToList();

                    if (meaiContents.Count > 0)
                    {
                        yield return new ChatMessage(role, meaiContents!);
                        continue;
                    }
                }

                yield return new ChatMessage(role, msg.Text ?? string.Empty);
            }
        }
    }

    private static AIContent? ConvertContent(Protocol.Requests.MessageContent content)
    {
        return content switch
        {
            Protocol.Requests.AgentKitTextContent tc => new Microsoft.Extensions.AI.TextContent(tc.Text),
            Protocol.Requests.AgentKitFunctionCallContent fc => new Microsoft.Extensions.AI.FunctionCallContent(
                fc.CallId, fc.Name,
                fc.Arguments is null ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object?>>(fc.Arguments.RootElement.GetRawText())),
            Protocol.Requests.AgentKitFunctionResultContent fr => new Microsoft.Extensions.AI.FunctionResultContent(
                fr.CallId, fr.Result is null ? null : fr.Result.RootElement.GetRawText()),
            _ => null,
        };
    }

    private const string DefaultRepairPrompt =
        "你是一个 JSON 格式修复助手。你的任务是修复提供的 JSON 输出，使其符合指定的 JSON Schema。\n" +
        "\n严格遵守以下规则：\n" +
        "1. 仅修正格式错误（类型不匹配、缺少必需字段、JSON 语法错误）\n" +
        "2. 不要编造或补充原始输出中不存在的内容\n" +
        "3. 不要删除原始输出中已有的有效内容\n" +
        "4. 如果原始输出无法修复为合法格式，返回 null\n" +
        "5. 只返回修复后的 JSON，不要添加任何解释或标记";

    private static async Task<string?> AutoRepairAsync(
        IChatClient chatClient,
        string rawOutput,
        StructuredValidationResult validation,
        JsonDocument schema,
        string? customPrompt,
        CancellationToken cancellationToken)
    {
        var systemPrompt = customPrompt ?? DefaultRepairPrompt;
        var userMessage =
            "## JSON Schema\n" + schema.RootElement.GetRawText() + "\n\n" +
            "## 原始输出（有格式错误）\n" + rawOutput + "\n\n" +
            "## 校验错误\n" + string.Join("\n", validation.Errors ?? []);

        var messages = new ChatMessage[]
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage),
        };

        var options = new ChatOptions { Temperature = 0f };
        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);
        return response.Text;
    }
}
