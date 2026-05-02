# Microsoft Agent Framework (MAF) API 参考文档

> 基于 `E:\Code\Ye\agent-framework-main\agent-framework-main\dotnet\src\` 源码分析
>
> 版本：当前 main 分支（2026-05-01 分析）
>
> 用途：为 AgentKit 的 MAF 适配层提供 API 参考

---

## 目录

1. [核心架构概览](#1-核心架构概览)
2. [核心抽象层 (Microsoft.Agents.AI.Abstractions)](#2-核心抽象层)
3. [ChatClientAgent 实现层 (Microsoft.Agents.AI)](#3-chatclientagent-实现层)
4. [ChatHistoryProvider 历史消息体系](#4-chathistoryprovider-历史消息体系)
5. [AIContextProvider 上下文注入体系](#5-aicontextprovider-上下文注入体系)
6. [Skills 体系](#6-skills-体系)
7. [Compaction 上下文压缩](#7-compaction-上下文压缩)
8. [Tool Approval 审批机制](#8-tool-approval-审批机制)
9. [ContinuationToken 续跑机制](#9-continuationtoken-续跑机制)
10. [PerServiceCall 持久化装饰器](#10-perservicecall-持久化装饰器)
11. [类型关系图](#11-类型关系图)
12. [AgentKit 适配要点](#12-agentkit-适配要点)

---

## 1. 核心架构概览

MAF 采用分层架构：

```
┌─────────────────────────────────────────────┐
│  应用层 (AgentPlatform / AgentKit)          │
├─────────────────────────────────────────────┤
│  Microsoft.Agents.AI                        │
│  ChatClientAgent, Skills, Compaction        │
├─────────────────────────────────────────────┤
│  Microsoft.Agents.AI.Abstractions          │
│  AIAgent, AgentSession, Providers           │
├─────────────────────────────────────────────┤
│  Microsoft.Extensions.AI                    │
│  IChatClient, ChatMessage, AITool           │
└─────────────────────────────────────────────┘
```

**项目结构**：
- `Microsoft.Agents.AI.Abstractions` — 核心抽象（AIAgent, AgentSession, Provider 契约）
- `Microsoft.Agents.AI` — ChatClientAgent 实现 + Skills + Compaction
- `Microsoft.Agents.AI.Workflows` — Workflow 编排、MCP 集成
- 各 AI 提供商项目：OpenAI, Anthropic, AzureAI, CopilotStudio 等

---

## 2. 核心抽象层

### 2.1 AIAgent

**文件**: `Microsoft.Agents.AI.Abstractions/AIAgent.cs`

抽象基类，所有 Agent 实现的根类型。

```csharp
public abstract class AIAgent
{
    // 标识
    protected abstract string? IdCore { get; }
    public string? Id => IdCore;
    public virtual string? Name { get; }
    public virtual string? Description { get; }

    // 运行（非流式）
    public Task<AgentResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<AgentResponse> RunAsync(
        string messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<AgentResponse> RunAsync(
        ChatMessage message,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);

    // 运行（流式）
    public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default);

    // Session 管理
    public ValueTask<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default);
    public ValueTask<JsonElement> SerializeSessionAsync(AgentSession session, ...);
    public ValueTask<AgentSession> DeserializeSessionAsync(JsonElement serializedState, ...);

    // RunContext（AsyncLocal 流式传递）
    public static AgentRunContext? CurrentRunContext { get; set; }

    // 服务解析
    public virtual object? GetService(Type serviceType, object? serviceKey = null);
}
```

**关键设计**：
- `CurrentRunContext` 是 `AsyncLocal<AgentRunContext?>`，在 Run 期间自动设置，供 PerServiceCall 等装饰器获取当前 Agent 和 Session
- `RunAsync` 的多个重载最终都调用 `RunCoreAsync`
- Session 可以由调用方传入，也可以自动创建

### 2.2 AgentSession

**文件**: `Microsoft.Agents.AI.Abstractions/AgentSession.cs`

会话抽象基类。

```csharp
public abstract class AgentSession
{
    public AgentSessionStateBag StateBag { get; }
    public virtual object? GetService(Type serviceType, object? serviceKey = null);
}
```

**关键设计**：
- `StateBag` 是 `AgentSessionStateBag`（字典），用于存储各 Provider 的会话状态
- `GetService` 支持运行时服务解析（TService 模式）

### 2.3 AgentSessionStateBag

**文件**: `Microsoft.Agents.AI.Abstractions/AgentSessionStateBag.cs`

会话状态存储，字典模式。

```csharp
public sealed class AgentSessionStateBag : Dictionary<string, JsonElement>
{
    // 通过 ProviderSessionState<T> 泛型辅助类进行类型安全的读写
}
```

**关键设计**：
- 键为 Provider 的 `StateKey`，值为序列化后的 `JsonElement`
- `ProviderSessionState<T>` 提供类型安全的封装：
  - `GetOrInitializeState(session)` — 从 StateBag 加载或初始化
  - `SetState(session, state)` — 写入 StateBag

### 2.4 AgentRunContext

**文件**: `Microsoft.Agents.AI.Abstractions/AgentRunContext.cs`

运行上下文，通过 `AsyncLocal` 在调用链中传递。

```csharp
public sealed record AgentRunContext(
    AIAgent Agent,
    AgentSession? Session,
    IEnumerable<ChatMessage> RequestMessages,
    AgentRunOptions? RunOptions);
```

**关键设计**：
- 在 `AIAgent.RunAsync` / `RunStreamingAsync` 开始时设置
- 在流式场景下，每次 yield 后需重新设置（base class 会还原）
- `ChatClientAgent.EnsureRunContextHasSession` 确保 Session 已解析

### 2.5 AgentResponse

**文件**: `Microsoft.Agents.AI.Abstractions/AgentResponse.cs`

非流式响应。

```csharp
public class AgentResponse
{
    public string? AgentId { get; set; }
    public string? ResponseId { get; }
    public string Text { get; }
    public IList<ChatMessage> Messages { get; }
    public ChatFinishReason? FinishReason { get; }
    public UsageDetails? Usage { get; }
    public ResponseContinuationToken? ContinuationToken { get; set; }
    public AdditionalPropertiesDictionary? AdditionalProperties { get; }
}
```

### 2.6 AgentResponseUpdate

**文件**: `Microsoft.Agents.AI.Abstractions/AgentResponseUpdate.cs`

流式增量响应。

```csharp
public class AgentResponseUpdate
{
    public string? AgentId { get; set; }
    public string? ResponseId { get; }
    public string? MessageId { get; }
    public ChatRole? Role { get; }
    public string Text { get; }
    public IList<AIContent> Contents { get; }
    public ChatFinishReason? FinishReason { get; }
    public ResponseContinuationToken? ContinuationToken { get; set; }
    public AdditionalPropertiesDictionary? AdditionalProperties { get; }
}
```

### 2.7 AgentRunOptions

**文件**: `Microsoft.Agents.AI.Abstractions/AgentRunOptions.cs`

运行选项基类。

```csharp
public class AgentRunOptions
{
    public ResponseContinuationToken? ContinuationToken { get; set; }
    public bool? AllowBackgroundResponses { get; set; }
    public ChatResponseFormat? ResponseFormat { get; set; }
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
    public virtual AgentRunOptions Clone();
}
```

---

## 3. ChatClientAgent 实现层

### 3.1 ChatClientAgent

**文件**: `Microsoft.Agents.AI/ChatClient/ChatClientAgent.cs`（~1100 行）

MAF 核心实现，包装 `IChatClient`。

```csharp
public sealed partial class ChatClientAgent : AIAgent
{
    // 构造函数
    public ChatClientAgent(
        IChatClient chatClient,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null);

    public ChatClientAgent(
        IChatClient chatClient,
        ChatClientAgentOptions? options,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null);

    // 核心属性
    public IChatClient ChatClient { get; }
    public ChatHistoryProvider? ChatHistoryProvider { get; }
    public IReadOnlyList<AIContextProvider>? AIContextProviders { get; }
}
```

**构造逻辑**：
1. 如果 `UseProvidedChatClientAsIs` 为 false（默认），通过 `WithDefaultAgentMiddleware` 装饰 chatClient
2. 自动插入 `FunctionInvokingChatClient`（工具循环）
3. 如果 `RequirePerServiceCallChatHistoryPersistence` 为 true，插入 `PerServiceCallChatHistoryPersistingChatClient`
4. `ChatHistoryProvider` 默认为 `InMemoryChatHistoryProvider`
5. 校验所有 Provider 的 `StateKeys` 唯一性

#### RunCoreAsync（非流式）

```
PrepareSessionAndMessagesAsync →
  chatClient.GetResponseAsync →
    NotifyProvidersOfNewMessagesAsync →
      返回 AgentResponse
```

**PrepareSessionAndMessagesAsync** 是核心准备方法：

1. **创建/恢复 Session**：如果调用方传入 null，自动创建 `ChatClientAgentSession`
2. **合并 ChatOptions**：运行级选项 > Agent 默认选项
3. **加载 ChatHistory**：如果未启用 PerServiceCall，通过 `ChatHistoryProvider.InvokingAsync` 加载
4. **调用 AIContextProviders**：逐个调用 `InvokingAsync`，累积 Instructions/Messages/Tools
5. **应用 ContinuationToken**：如果存在续跑令牌

#### RunCoreStreamingAsync（流式）

与非流式相同准备流程，然后：
1. 调用 `chatClient.GetStreamingResponseAsync`
2. 逐个 yield `AgentResponseUpdate`
3. 每次 yield 后需 `EnsureRunContextHasSession`（AsyncLocal 恢复）
4. 流结束后通知 Providers

### 3.2 ChatClientAgentSession

**文件**: `Microsoft.Agents.AI/ChatClient/ChatClientAgentSession.cs`

```csharp
public sealed class ChatClientAgentSession : AgentSession
{
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; internal set; }

    // 序列化/反序列化
    internal JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null);
    internal static ChatClientAgentSession Deserialize(JsonElement serializedState, ...);
}
```

**序列化格式**：
```json
{
  "conversationId": "xxx",
  "stateBag": { "key1": ..., "key2": ... }
}
```

**ConversationId 含义**：
- `null` — 框架管理历史（通过 ChatHistoryProvider）
- 非 null — 服务管理历史（AI Service 端存储）
- `"_agent_local_chat_history"` — PerServiceCall 的 sentinel 值（见第 10 节）

### 3.3 ChatClientAgentOptions

**文件**: `Microsoft.Agents.AI/ChatClient/ChatClientAgentOptions.cs`

```csharp
public sealed class ChatClientAgentOptions
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ChatOptions? ChatOptions { get; set; }
    public ChatHistoryProvider? ChatHistoryProvider { get; set; }
    public IEnumerable<AIContextProvider>? AIContextProviders { get; set; }
    public bool UseProvidedChatClientAsIs { get; set; }           // default: false
    public bool ClearOnChatHistoryProviderConflict { get; set; }   // default: true
    public bool WarnOnChatHistoryProviderConflict { get; set; }    // default: true
    public bool ThrowOnChatHistoryProviderConflict { get; set; }   // default: true
    public bool RequirePerServiceCallChatHistoryPersistence { get; set; } // default: false
    public ChatClientAgentOptions Clone();
}
```

**关键标记**：
- `RequirePerServiceCallChatHistoryPersistence`：启用 PerServiceCall 持久化装饰器。AgentPlatform 在有 Tools 时启用此选项。
- `UseProvidedChatClientAsIs`：跳过默认装饰器链，调用方自行管理 pipeline。

### 3.4 ChatClientAgentRunOptions

**文件**: `Microsoft.Agents.AI/ChatClient/ChatClientAgentRunOptions.cs`

```csharp
public sealed class ChatClientAgentRunOptions : AgentRunOptions
{
    public ChatOptions? ChatOptions { get; set; }
    public Func<IChatClient, IChatClient>? ChatClientFactory { get; set; }
}
```

- `ChatOptions`：运行级聊天参数，与 Agent 默认参数合并
- `ChatClientFactory`：可替换运行时的 chatClient（如添加特定装饰器）

---

## 4. ChatHistoryProvider 历史消息体系

### 4.1 ChatHistoryProvider（抽象基类）

**文件**: `Microsoft.Agents.AI.Abstractions/ChatHistoryProvider.cs`（~478 行）

```csharp
public abstract class ChatHistoryProvider : AIContextProvider
{
    // 加载历史 → 返回合并后的消息列表
    public virtual async Task<IEnumerable<ChatMessage>> InvokingAsync(
        InvokingContext context, CancellationToken cancellationToken = default);

    // 持久化新消息
    public virtual async Task InvokedAsync(
        InvokedContext context, CancellationToken cancellationToken = default);

    // 需子类实现
    protected abstract ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(...);
    protected abstract ValueTask StoreChatHistoryAsync(...);

    // 消息过滤
    public virtual Func<IEnumerable<ChatMessage>, IEnumerable<ChatMessage>>? ProvideInputMessageFilter { get; }
    public virtual Func<IEnumerable<ChatMessage>, IEnumerable<ChatMessage>>? StoreInputRequestMessageFilter { get; }
}
```

**InvokingAsync 流程**：
1. 调用 `ProvideChatHistoryAsync` 加载历史
2. 通过 `ProvideInputMessageFilter` 过滤
3. 给历史消息打上 `AgentRequestMessageSourceType.ChatHistory` 标记
4. 合并历史 + 输入消息，返回

**InvokedAsync 流程**：
1. 过滤请求消息（排除已在历史中的）
2. 过滤响应消息
3. 调用 `StoreChatHistoryAsync` 持久化

### 4.2 InMemoryChatHistoryProvider

**文件**: `Microsoft.Agents.AI/ChatClient/InMemoryChatHistoryProvider.cs`

默认的内存实现。StateKey 为 `"InMemoryChatHistoryProvider"`。

---

## 5. AIContextProvider 上下文注入体系

### 5.1 AIContextProvider（抽象基类）

**文件**: `Microsoft.Agents.AI.Abstractions/AIContextProvider.cs`（~511 行）

```csharp
public abstract class AIContextProvider
{
    // 状态键（用于 Session StateBag 隔离）
    public virtual IReadOnlyList<string> StateKeys => [];

    // 运行前注入
    public virtual async ValueTask<AIContext> InvokingAsync(
        InvokingContext context, CancellationToken cancellationToken = default);

    // 运行后回调
    public virtual ValueTask InvokedAsync(
        InvokedContext context, CancellationToken cancellationToken = default);

    // 需子类实现
    protected virtual ValueTask<AIContext> ProvideAIContextAsync(...);
    protected virtual ValueTask StoreAIContextAsync(...);

    // 服务解析
    public virtual object? GetService(Type serviceType, object? serviceKey = null);
}
```

### 5.2 AIContext

```csharp
public class AIContext
{
    public string? Instructions { get; set; }
    public IEnumerable<ChatMessage>? Messages { get; set; }
    public IEnumerable<AITool>? Tools { get; set; }
}
```

**调用链**：
1. Agent 初始化 `AIContext { Instructions, Messages, Tools }`（来自 ChatOptions）
2. 逐个调用 Provider 的 `InvokingAsync`，每个 Provider 返回新的 `AIContext`
3. 最终的 `AIContext` 覆盖 ChatOptions 的 Instructions/Tools/Messages
4. 运行完成后逐个调用 `InvokedAsync`

### 5.3 InvokingContext / InvokedContext

```csharp
// InvokingContext
public InvokingContext(AIAgent agent, AgentSession session, AIContext aiContext);
public AIAgent Agent { get; }
public AgentSession Session { get; }
public AIContext AIContext { get; }

// InvokedContext
public InvokedContext(AIAgent agent, AgentSession session,
    IEnumerable<ChatMessage> requestMessages, IEnumerable<ChatMessage> responseMessages);
public InvokedContext(AIAgent agent, AgentSession session,
    IEnumerable<ChatMessage> requestMessages, Exception exception);
```

---

## 6. Skills 体系

### 6.1 AgentSkillsProvider

**文件**: `Microsoft.Agents.AI/Skills/AgentSkillsProvider.cs`（~414 行）

`AIContextProvider` 子类，实现渐进式披露（Progressive Disclosure）。

**三个工具**：
1. `load_skill` — 加载 Skill 描述和可用资源列表
2. `read_skill_resource` — 读取指定资源内容
3. `run_skill_script` — 执行 Skill 脚本

**特性**：
- 动态构建 `AIFunction` 工具
- 支持 `ScriptApproval` 审批门控（通过 `ApprovalRequiredAIFunction` 包装）
- Prompt 模板支持 `{skills}`, `{resource_instructions}`, `{script_instructions}` 占位符
- 每个 Skill 包含 `AgentSkill`, `AgentSkillResource`, `AgentSkillScript` 数据模型

### 6.2 AgentSkillsProviderBuilder

**文件**: `Microsoft.Agents.AI/Skills/AgentSkillsProviderBuilder.cs`（~246 行）

流式构建器：

```csharp
builder
    .UseFileSkill("path/to/skill.json")
    .UseSkills(skills)
    .UsePromptTemplate(customTemplate)
    .UseScriptApproval()
    .UseFileScriptRunner(runner)
    .UseFilter(filter)
    .Build();
```

### 6.3 AgentSkillsProviderOptions

```csharp
public class AgentSkillsProviderOptions
{
    public bool ScriptApproval { get; set; }     // 是否需要审批脚本执行
    public bool DisableCaching { get; set; }      // 是否禁用缓存
    public string? SkillsInstructionPrompt { get; set; } // 自定义指令
}
```

---

## 7. Compaction 上下文压缩

### 7.1 CompactionProvider

**文件**: `Microsoft.Agents.AI/Compaction/CompactionProvider.cs`（~210 行）

`AIContextProvider` 子类，在每次 Agent 调用前压缩消息。

```csharp
public sealed class CompactionProvider : AIContextProvider
{
    public CompactionProvider(
        CompactionStrategy compactionStrategy,
        string? stateKey = null,
        ILoggerFactory? loggerFactory = null);

    // StateKeys: [stateKey or strategy.GetType().Name]

    // InvokingCoreAsync:
    // 1. 跳过有 ConversationId 的 session（服务管理历史）
    // 2. 构建 CompactionMessageIndex
    // 3. 调用 compactionStrategy.CompactAsync
    // 4. 返回压缩后的 AIContext

    // 静态方法：独立压缩
    public static async Task<IEnumerable<ChatMessage>> CompactAsync(
        CompactionStrategy compactionStrategy,
        IEnumerable<ChatMessage> messages, ...);
}
```

**内部状态**：
```csharp
internal sealed class State
{
    public List<CompactionMessageGroup> MessageGroups { get; set; } = [];
}
```

### 7.2 CompactionStrategy（抽象基类）

**文件**: `Microsoft.Agents.AI/Compaction/CompactionStrategy.cs`

```csharp
public abstract class CompactionStrategy
{
    protected CompactionTrigger Trigger { get; }  // 何时触发压缩
    protected CompactionTrigger Target { get; }   // 何时停止压缩

    public async ValueTask<bool> CompactAsync(CompactionMessageIndex index, ...);
    protected abstract ValueTask<bool> CompactCoreAsync(...);
}
```

**CompactionTrigger** 是委托：`Func<CompactionMessageIndex, bool>`

**CompactionTriggers** 工厂：
- `MessagesExceed(threshold)` — 消息数超限
- `TurnsExceed(threshold)` — 轮次超限
- `GroupsExceed(threshold)` — 分组超限
- `TokensExceed(threshold)` — Token 数超限
- `HasToolCalls()` — 存在工具调用
- `All(trigger1, trigger2, ...)` — 所有触发器同时满足

### 7.3 内置策略

| 策略 | 说明 |
|------|------|
| `SummarizationCompactionStrategy` | LLM 摘要压缩，保护最近 N 组消息 |
| `SlidingWindowCompactionStrategy` | 滑动窗口，移除最旧的消息 |
| `TruncationCompactionStrategy` | 截断，仅保留最近 N 组 |
| `ToolResultCompactionStrategy` | 仅压缩工具调用结果 |
| `PipelineCompactionStrategy` | 策略流水线，顺序执行多个策略 |

### 7.4 SummarizationCompactionStrategy 详解

```csharp
public sealed class SummarizationCompactionStrategy : CompactionStrategy
{
    public SummarizationCompactionStrategy(
        IChatClient chatClient,              // 摘要用的 chatClient（建议用小模型）
        CompactionTrigger trigger,
        int minimumPreservedGroups = 8,      // 最少保留的非系统消息组数
        string? summarizationPrompt = null,  // 自定义摘要 prompt
        CompactionTrigger? target = null);
}
```

**流程**：
1. 评估 Trigger → 不触发则跳过
2. 收集最旧的非系统消息组（不超过 `maxSummarizable = total - minimumPreserved`）
3. 逐组标记为 excluded，同时评估 Target → 达标即停止
4. 调用 `chatClient.GetResponseAsync` 生成摘要
5. 插入摘要组（`CompactionGroupKind.Summary`）到第一个被压缩组的位置
6. 失败时恢复所有 excluded 组

---

## 8. Tool Approval 审批机制

### 8.1 概览

MAF 的 Tool Approval 通过 `FunctionInvokingChatClient` 内置机制实现：

1. **标记需要审批的工具**：通过 `ApprovalRequiredAIFunction` 包装原始 `AIFunction`
2. **运行时触发**：当 LLM 调用需要审批的工具时，`FunctionInvokingChatClient` 生成 `ToolApprovalRequestContent`
3. **外部审批**：调用方从响应中提取 `ToolApprovalRequestContent`，展示给用户
4. **恢复执行**：构造 `ToolApprovalResponseContent(true/false)`，作为新消息送入 Session 继续运行

### 8.2 ToolApprovalRequestContent / ToolApprovalResponseContent

属于 `Microsoft.Extensions.AI` 命名空间（底层库），MAF 通过 `FunctionInvokingChatClient` 使用。

```csharp
// ToolApprovalRequestContent
public class ToolApprovalRequestContent : AIContent
{
    public string RequestId { get; }
    public FunctionCallContent ToolCall { get; }  // 包含工具名和参数
}

// ToolApprovalResponseContent
public class ToolApprovalResponseContent : AIContent
{
    public string RequestId { get; }
    public bool Approved { get; }
}
```

### 8.3 AgentPlatform 的审批流程

AgentPlatform 在 `MafAgentRunService` 中实现了完整的审批流程：

1. 运行 Agent → 获取 `AgentResponse`
2. 从响应消息中提取 `ToolApprovalRequestContent`
3. 通过 `RuntimeApprovalPayloadFactory` 创建审批请求
4. 暂停运行，发布 `ApprovalRequestedEvent`
5. 等待外部审批（`ApprovalResolvedEvent`）
6. 构造 `ToolApprovalResponseContent` → 作为恢复消息
7. 调用 `RunAsync` 继续执行（MAF 的 `FunctionInvokingChatClient` 自动处理）

### 8.4 ApprovalRequiredAIFunction

`Microsoft.Agents.AI` 中的包装器，将普通 `AIFunction` 标记为需要审批：

```csharp
internal sealed class ApprovalRequiredAIFunction : AIFunction
{
    private readonly AIFunction _innerFunction;
    // 包装所有属性，但标记为需要审批
}
```

AgentPlatform 通过 `capabilityContext.RuntimeProperties[HasApprovalRequiredTools]` 检测是否有需要审批的工具，如果有则设置 `AllowMultipleToolCalls = false`。

---

## 9. ContinuationToken 续跑机制

### 9.1 概览

MAF 支持两种续跑场景：
1. **流式恢复**：流式连接中断后，从断点恢复
2. **后台响应轮询**：长时间运行的任务，通过轮询获取结果

### 9.2 ChatClientAgentContinuationToken

**文件**: `Microsoft.Agents.AI/ChatClient/ChatClientAgentContinuationToken.cs`（internal 类）

```csharp
internal class ChatClientAgentContinuationToken : ResponseContinuationToken
{
    internal ResponseContinuationToken InnerToken { get; }       // 底层 IChatClient 的 token
    internal IEnumerable<ChatMessage>? InputMessages { get; set; } // 输入消息（用于流式恢复）
    internal IReadOnlyList<ChatResponseUpdate>? ResponseUpdates { get; set; } // 已收到的更新

    public override ReadOnlyMemory<byte> ToBytes();              // 自定义二进制序列化
    internal static ChatClientAgentContinuationToken FromToken(ResponseContinuationToken token);
}
```

**序列化格式**：JSON-in-binary
```json
{
  "type": "chatClientAgentContinuationToken",
  "innerToken": "...",
  "inputMessages": [...],
  "responseUpdates": [...]
}
```

### 9.3 使用方式

```csharp
// 第一次运行
var response = await agent.RunStreamingAsync(messages, session);
// 如果中断，response 中的 ContinuationToken 可用于恢复

// 恢复运行
var runOptions = new ChatClientAgentRunOptions
{
    ContinuationToken = response.ContinuationToken
};
var resumedUpdates = await agent.RunStreamingAsync([], session, runOptions);
```

---

## 10. PerServiceCall 持久化装饰器

### 10.1 概览

`PerServiceCallChatHistoryPersistingChatClient` 是 `DelegatingChatClient` 子类，在 `FunctionInvokingChatClient` 循环中每次服务调用前后处理历史持久化。

**适用场景**：当 Agent 有 Tools 时，`FunctionInvokingChatClient` 会进行多轮调用。如果只在运行结束时持久化，中途崩溃会丢失已执行的工具调用。

### 10.2 Sentinel 机制

```csharp
internal const string LocalHistoryConversationId = "_agent_local_chat_history";
```

**工作原理**：
1. 当框架管理历史（无真实 ConversationId）时，装饰器在每次服务调用后设置 sentinel ConversationId
2. `FunctionInvokingChatClient` 看到非空 `ConversationId` → 认为是服务管理历史 → 在迭代间清理累积历史
3. 装饰器在转发请求前剥离 sentinel → 底层模型永远看不到 sentinel

### 10.3 处理流程

**非流式 (GetResponseAsync)**：
```
1. StripLocalHistoryConversationId(options)
2. 如果是服务管理/续跑：直接转发
3. 否则：LoadChatHistoryAsync → 合并历史 → 调用底层 client
4. NotifyProvidersOfNewMessagesAsync（每次调用后）
5. 设置 sentinel ConversationId 或更新真实 ConversationId
```

**流式 (GetStreamingResponseAsync)**：
```
1. 同上准备
2. 收集所有 updates
3. 在每个 update 上设置 sentinel ConversationId（如果不是服务管理）
4. 流结束后 NotifyProvidersOfNewMessagesAsync
```

### 10.4 AgentPlatform 的使用

AgentPlatform 在 `RuntimeComposer.ComposeCoreAsync` 中：
```csharp
RequirePerServiceCallChatHistoryPersistence = capabilityContext.Tools.Count > 0,
```

当有 Tools 时自动启用 PerServiceCall 持久化。

---

## 11. 类型关系图

```
AIAgent (抽象基类)
  └── ChatClientAgent (核心实现)
        ├── ChatClientAgentOptions
        │     ├── Id, Name, Description
        │     ├── ChatOptions (温度、模型、工具等)
        │     ├── ChatHistoryProvider (历史消息)
        │     ├── AIContextProviders[] (上下文注入)
        │     ├── UseProvidedChatClientAsIs
        │     └── RequirePerServiceCallChatHistoryPersistence
        ├── ChatClientAgentSession : AgentSession
        │     ├── ConversationId (null=框架管理, 非null=服务管理, sentinel=PerServiceCall)
        │     └── StateBag : AgentSessionStateBag (Provider 状态存储)
        ├── ChatClientAgentRunOptions : AgentRunOptions
        │     ├── ChatOptions
        │     └── ChatClientFactory
        ├── IChatClient ChatClient
        │     ├── FunctionInvokingChatClient (工具循环)
        │     └── PerServiceCallChatHistoryPersistingChatClient (历史持久化装饰器)
        └── ChatClientAgentContinuationToken : ResponseContinuationToken
              ├── InnerToken
              ├── InputMessages
              └── ResponseUpdates

AIContextProvider (抽象基类)
  ├── InvokingAsync → AIContext { Instructions, Messages, Tools }
  ├── InvokedAsync → 处理结果
  └── StateKeys[] (Session 状态隔离)

ChatHistoryProvider : AIContextProvider
  ├── InvokingAsync → ProvideChatHistoryAsync → 合并历史+输入
  └── InvokedAsync → StoreChatHistoryAsync → 持久化新消息

CompactionProvider : AIContextProvider
  └── CompactionStrategy
        ├── SummarizationCompactionStrategy (LLM 摘要)
        ├── SlidingWindowCompactionStrategy (滑动窗口)
        ├── TruncationCompactionStrategy (截断)
        ├── ToolResultCompactionStrategy (工具结果压缩)
        └── PipelineCompactionStrategy (策略流水线)

AgentSkillsProvider : AIContextProvider
  ├── load_skill (渐进式披露)
  ├── read_skill_resource
  └── run_skill_script (可配置审批)
```

---

## 12. AgentKit 适配要点

### 12.1 Session 桥接

AgentKit 需要实现 `ChatHistoryProvider` 子类（`MafChatHistoryProviderBridge`）：
- `ProvideChatHistoryAsync` → 调用 `IAgentKitSessionStore.LoadMessagesAsync`
- `StoreChatHistoryAsync` → 调用 `IAgentKitSessionStore.AppendMessagesAsync`
- 使用 `StateBag` 存储加载游标

### 12.2 PerServiceCall 场景

当 AgentKit 配置的 Agent 有 Tools 时：
- 设置 `RequirePerServiceCallChatHistoryPersistence = true`
- `UseProvidedChatClientAsIs = false`（让 MAF 自动插入装饰器）
- 桥接层需处理 sentinel ConversationId（不存储到 Session）

### 12.3 Tool Approval 桥接

- 从 `AgentResponse.Messages` 中提取 `ToolApprovalRequestContent`
- 转换为 AgentKit 协议层的 `ApprovalRequested` 事件
- 接收外部审批决策 → 构造 `ToolApprovalResponseContent` → 作为恢复消息

### 12.4 Compaction 集成

- 将 AgentKit 的压缩配置映射为 MAF 的 `CompactionStrategy` + `CompactionTrigger`
- 通过 `CompactionProvider` 作为 `AIContextProvider` 注入
- `stateKey` 使用 `maf-compaction:{agentVersionId}` 避免冲突

### 12.5 Skills 桥接

- AgentKit 的 Skill 定义 → MAF 的 `AgentSkill` / `AgentSkillResource` / `AgentSkillScript`
- 通过 `AgentSkillsProviderBuilder` 构建
- 审批门控通过 `UseScriptApproval()` 启用

### 12.6 ContinuationToken 处理

- AgentKit 的 `AgentKitRunRequest.ContinuationToken` → MAF 的 `ResponseContinuationToken`
- 通过 `ChatClientAgentRunOptions.ContinuationToken` 传入
- MAF 内部自动处理 `ChatClientAgentContinuationToken` 的序列化/反序列化

### 12.7 Structured Output

AgentPlatform 使用二段式（`StructuredOutputMiddleware`）：
1. 第一段：正常运行（Tools 启用）→ 收集结果
2. 第二段：关闭 Tools，设置 `ChatResponseFormatJson`，发送收集结果要求结构化输出

AgentKit 需在 Core 层复现此流程，或通过 MAF 的 `ChatOptions.ResponseFormat` 配合指令实现。
