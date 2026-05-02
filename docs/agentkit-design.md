# AgentKit 设计方案

## 1. 文档目标

本文档定义 `AgentKit` 的设计方案。

`AgentKit` 基于 Microsoft Agent Framework (MAF)，将业务项目中反复出现的能力收敛为独立库：

- Agent 定义协议
- 运行编排与流式事件投影
- Session 创建、快照、恢复
- Tool / Skills / MCP 能力装配
- Tool Approval 与恢复
- Structured Output
- 安全重试
- 存储协议

业务项目只需：
1. 维护 Agent 定义配置
2. 注册模型客户端解析器
3. 实现或选择存储实现
4. 消费统一事件流

不需要关心 MAF 内部消息结构、ToolApproval 恢复细节、Session 快照序列化、MCP 连接管理等。

## 2. 设计原则

### 2.1 MAF First

直接复用 MAF 官方能力，不做重复实现：

| MAF 能力 | AgentKit 用法 |
|----------|-------------|
| `ChatClientAgent` | 核心运行时 |
| `AgentSession` | 会话管理 |
| `ChatHistoryProvider` | 历史桥接 |
| `ToolApprovalRequestContent` | 审批恢复 |
| `AgentSkillsProvider` | 技能装配 |
| `ChatResponseFormatJson` | 结构化输出 |
| `ResponseContinuationToken` | 续跑令牌 |
| `CompactionProvider` | 消息压缩 |

明确不做：不自建 Session 协议、Tool Approval 协议、Skills Runtime、MCP Runtime。

### 2.2 协议与实现分离

对外暴露稳定协议（纯类型定义，零外部依赖）：

- `AgentKitDefinition` — Agent 定义
- `RunRequest` — 运行请求
- `RunResult` — 运行结果
- `KitEvent` — 统一事件
- `ISessionStore` / `IEventStore` / `IApprovalStore` — 存储协议
- `IToolRegistry` / `ISkillRegistry` / `IMcpServerRegistry` — 能力注册

对内封装 MAF 实现（Composer、SessionAdapter、EventProjector 等），业务项目不感知。

### 2.3 能力分族

- **Tool**：本地函数工具
- **Skill**：文件型脚本能力
- **MCP**：远程 Server 工具

三者独立注册、独立生命周期，不在内部互相降格。

### 2.4 存储协议分层

定义 `ISessionStore`、`IEventStore`、`IApprovalStore` 三个独立接口，不做单体仓储。内置 `InMemoryStore` 用于开发测试。

## 3. 范围

### 3.1 已实现

- Agent 定义协议（Key/Name/Model/SystemPrompt/Tools/Skills/MCP/StructuredOutput/Execution）
- 非流式运行 + 流式运行
- Session 创建、恢复、序列化、持久化
- Function Tool 注册与装配
- Skill 文件发现与装配
- MCP Server 注册（stdio / streamable-http）
- Tool Approval 检测与恢复
- Structured Output 校验 + 修复策略
- 安全重试（指数退避，仅无可见输出时）
- 续跑令牌
- 事件投影（RunStarted/Delta/Tool/Approval/Completed/Failed）
- 事件持久化
- ChatHistoryProvider 桥接
- InMemory 存储实现
- DI 一站式注册

### 3.2 未实现

- MCP 运行时连接（委托外部实现）
- Skills 脚本执行引擎（委托外部实现）
- CompactionProvider 集成（组件已定义，未接入管道）
- 事件 Phase 2 扩展（TokenUsage、Cost 等）

## 4. 解决方案与项目拆分

### 4.1 项目清单

```
src/
  AgentKit.Protocol/                    # 协议层：纯类型定义，零外部依赖
  AgentKit.Abstractions/               # 抽象层：扩展点接口
  AgentKit.Capabilities/               # 能力注册：Tool / Skill / MCP
  AgentKit.Storage.Abstractions/       # 存储协议
  AgentKit.Storage.InMemory/           # 内存存储实现
  AgentKit.Core/                       # 编排层：Runner / Validator / Coordinator
  AgentKit.Maf/                        # MAF 适配层
  AgentKit.Extensions.DependencyInjection/  # DI 扩展
tests/
  AgentKit.Protocol.Tests/
  AgentKit.Storage.InMemory.Tests/
  AgentKit.Core.Tests/
  AgentKit.Maf.Tests/
```

### 4.2 各项目职责

**AgentKit.Protocol** — 纯协议定义，零外部依赖

包含：枚举、定义类型、请求类型、结果类型、会话类型、事件类型。

**AgentKit.Abstractions** — 扩展点接口

包含：`IRunner`、`IDefinitionResolver`、`IModelClientResolver`、`IStructuredTypeResolver`、`IStructuredOutputRepairStrategy`、`IEventPublisher`。

**AgentKit.Capabilities** — 能力注册

包含：`IToolRegistry` + `InMemoryToolRegistry`、`ISkillRegistry` + `InMemorySkillRegistry`、`IMcpServerRegistry` + `InMemoryMcpServerRegistry`。

**AgentKit.Storage.Abstractions** — 存储协议

包含：`ISessionStore`、`IEventStore`、`IApprovalStore` 及 `StoredSession`、`StoredMessage`、`StoredEvent`、`StoredApproval`、`StoredApprovalDecision`。

**AgentKit.Storage.InMemory** — 内存存储

`InMemoryStore` 同时实现三个存储接口，用于开发和测试。

**AgentKit.Core** — 编排层（不依赖 MAF）

包含：`Runner`、`IExecutionPipeline`、`PipelineResult`、`ResultAggregator`、`DefinitionValidator`、`RunRequestValidator`、`RetryCoordinator`、`ApprovalCoordinator`、`SessionCoordinator`、`StructuredTypeResolver`。

**AgentKit.Maf** — MAF 适配层

包含：`MafExecutionPipeline`、`Composer`、`SessionAdapter`、`EventProjector`、`ChatHistoryProviderBridge`、`ApprovalResumeFactory`、`ContinuationTokenConverter`、`StructuredOutputValidator`、`StructuredOutputCoordinator`、`ChatOptionsFactory`、`DefinitionCache`、`CompactionFactory`、`SkillProviderFactory`、`McpToolsetFactory`、`McpLifecycleManager`。

**AgentKit.Extensions.DependencyInjection** — DI 扩展

提供 `AddAgentKit()`、`AddAgentKitCore()`、`AddAgentKitMaf()` 一站式注册。

### 4.3 项目引用关系

```
AgentKit.Protocol          → 无引用
AgentKit.Abstractions      → Protocol
AgentKit.Capabilities      → Protocol
AgentKit.Storage.Abstractions → 无引用
AgentKit.Storage.InMemory  → Storage.Abstractions
AgentKit.Core              → Protocol, Abstractions, Capabilities, Storage.Abstractions
AgentKit.Maf               → Protocol, Abstractions, Capabilities, Storage.Abstractions, MAF NuGet
AgentKit.Extensions.DependencyInjection → Core, Maf, Capabilities, Storage.InMemory
```

关键约束：**Core 不直接依赖 MAF**，通过 `IExecutionPipeline` 接口解耦。

## 5. 协议设计

### 5.1 枚举

**RunState** — 运行状态

```csharp
public enum RunState
{
    Running,                    // 运行中
    Completed,                  // 正常完成
    CompletedWithToolCalls,     // 完成并调用了工具
    CompletedWithApproval,      // 等待审批
    CompletedWithContinuation,  // 需要续跑
    Failed,                     // 运行失败
    FailedContentFilter,        // 内容过滤
    FailedTokenLimit,           // Token 超限
}
```

**FailureKind** — 失败类型

```csharp
public enum FailureKind
{
    Unknown,
    ModelCallFailed,
    ToolExecutionFailed,
    StructuredOutputValidationFailed,
    ApprovalTimeout,
    McpConnectionFailed,
    SessionRestoreFailed,
    DefinitionResolutionFailed,
    ConfigurationInvalid,
    ContentFilter,
    TokenLimitExceeded,
}
```

**ToolKind** — 工具类型：`Function`、`Skill`、`Mcp`

**ApprovalTargetKind** — 审批目标：`FunctionTool`、`McpTool`、`SkillScript`

**StructuredFailureKind** — 结构化失败：`Empty`、`InvalidJson`、`InvalidSchema`、`SchemaMismatch`

**MessageRole** — 消息角色：`System`、`User`、`Assistant`、`Tool`

### 5.2 Agent 定义

```csharp
public sealed class AgentKitDefinition
{
    public required string Key { get; init; }                      // 定义唯一键
    public required string Name { get; init; }                     // 显示名称
    public string? Description { get; init; }                      // 描述
    public string? Version { get; init; }                          // 版本号
    public required ModelDefinition Model { get; init; }           // 模型配置
    public string? SystemPrompt { get; init; }                     // 系统提示词
    public ExecutionDefinition Execution { get; init; } = new();   // 执行策略
    public StructuredOutputDefinition? StructuredOutput { get; init; }
    public IReadOnlyList<ToolReference> Tools { get; init; } = [];
    public IReadOnlyList<SkillSetDefinition> Skills { get; init; } = [];
    public IReadOnlyList<McpServerReference> McpServers { get; init; } = [];
    public JsonDocument? Metadata { get; init; }
}
```

**ModelDefinition** — 模型配置

```csharp
public sealed class ModelDefinition
{
    public required string Provider { get; init; }        // 提供商标识
    public required string ModelId { get; init; }         // 模型 ID
    public string? Endpoint { get; init; }                // API 端点
    public string? CredentialKey { get; init; }           // 凭证密钥
    public bool SupportsFunctionTools { get; init; } = true;
    public bool SupportsStructuredOutput { get; init; } = true;
    public decimal? Temperature { get; init; }
    public decimal? TopP { get; init; }
    public int? MaxOutputTokens { get; init; }
}
```

**ExecutionDefinition** — 执行策略

```csharp
public sealed class ExecutionDefinition
{
    public bool StreamingEnabled { get; init; } = true;
    public bool AllowMultipleToolCalls { get; init; }
    public bool? AllowBackgroundResponses { get; init; }
    public bool PerServiceCallPersistence { get; init; }
    public bool? ModelSupportsToolsWithStructuredOutput { get; init; }
    public RetryPolicyDefinition Retry { get; init; } = new();
}
```

**RetryPolicyDefinition** — 重试策略

```csharp
public sealed class RetryPolicyDefinition
{
    public bool Enabled { get; init; }
    public int MaxAttempts { get; init; } = 1;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
    public bool RetryStructuredFinalization { get; init; } = true;
    public bool RetryBeforeVisibleOutputOnly { get; init; } = true;
}
```

**ToolReference** — 工具引用

```csharp
public sealed class ToolReference
{
    public required string Key { get; init; }
    public bool Enabled { get; init; } = true;
    public bool RequiresApproval { get; init; }
    public string? ApprovalReason { get; init; }
}
```

**SkillSetDefinition** — 技能集定义

```csharp
public sealed class SkillSetDefinition
{
    public required string Key { get; init; }
    public IReadOnlyList<string> Paths { get; init; } = [];
    public bool ScriptApproval { get; init; }
    public bool DisableCaching { get; init; }
    public string? PromptTemplate { get; init; }
    public SkillFileSourceOptions? FileSourceOptions { get; init; }
}
```

**McpServerReference** — MCP 服务引用

```csharp
public sealed class McpServerReference
{
    public required string Key { get; init; }
    public bool Enabled { get; init; } = true;
    public bool RequiresApproval { get; init; }
    public string? ApprovalReason { get; init; }
}
```

**StructuredOutputDefinition** — 结构化输出

```csharp
public sealed class StructuredOutputDefinition
{
    public required string Name { get; init; }
    public string? Version { get; init; }
    public string? TargetTypeName { get; init; }           // 用于类型反序列化
    public JsonDocument? Schema { get; init; }             // JSON Schema
    public bool StrictValidation { get; init; } = true;
    public string? RepairStrategyKey { get; init; }        // 修复策略键
}
```

### 5.3 运行请求

```csharp
public sealed class RunRequest
{
    public string? Input { get; init; }                              // 输入文本
    public IReadOnlyList<Message> Messages { get; init; } = [];      // 输入消息列表
    public SessionState? Session { get; init; }                      // 会话状态
    public ApprovalDecision? ApprovalDecision { get; init; }         // 审批决策
    public string? ContinuationToken { get; init; }                  // 续跑令牌
    public JsonDocument? Context { get; init; }
    public JsonDocument? Metadata { get; init; }
}
```

**Message** — 输入消息

```csharp
public sealed class Message
{
    public required MessageRole Role { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<MessageContent>? Contents { get; init; }
    public string? Source { get; init; }
}
```

**MessageContent** — 消息内容（多态）

```csharp
public abstract record MessageContent { public required string ContentType { get; init; } }
public sealed record AgentKitTextContent : MessageContent { public required string Text { get; init; } }
public sealed record AgentKitFunctionCallContent : MessageContent { public required string CallId { get; init; }; public required string Name { get; init; }; public required JsonDocument Arguments { get; init; } }
public sealed record AgentKitFunctionResultContent : MessageContent { public required string CallId { get; init; }; public required string Name { get; init; }; public required JsonDocument Result { get; init; }; public bool IsError { get; init; } }
```

### 5.4 运行结果

```csharp
public sealed class RunResult
{
    public required RunState RunState { get; init; }
    public string? FinalText { get; init; }
    public JsonDocument? StructuredPayload { get; init; }
    public StructuredValidationResult? StructuredValidation { get; init; }
    public SessionState? SessionState { get; init; }
    public PendingApproval? PendingApproval { get; init; }
    public string? ContinuationToken { get; init; }
    public int AttemptsUsed { get; init; }
    public Failure? Failure { get; init; }
}
```

**Failure** — 失败信息

```csharp
public sealed class Failure
{
    public required FailureKind Kind { get; init; }
    public required string Message { get; init; }
    public string? Detail { get; init; }
    public JsonDocument? Context { get; init; }
}
```

**StructuredValidationResult** — 结构化校验结果

```csharp
public sealed class StructuredValidationResult
{
    public bool IsValid { get; init; }
    public StructuredFailureKind? FailureKind { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public string? RawOutput { get; init; }
}
```

### 5.5 会话

```csharp
public sealed class SessionState
{
    public required string SessionId { get; init; }
    public JsonElement? AgentSessionData { get; init; }     // MAF 序列化快照
    public ResumptionContext ResumptionContext { get; init; } = new();
}

public sealed class ResumptionContext
{
    public IReadOnlyList<PendingApproval> PendingApprovals { get; init; } = [];
    public long Sequence { get; init; }
    public string? LastRunId { get; init; }
    public bool IsDirty { get; init; }
}
```

### 5.6 审批

```csharp
public sealed class PendingApproval
{
    public required string RequestId { get; init; }
    public required string CallId { get; init; }
    public required string TargetName { get; init; }
    public required ApprovalTargetKind TargetKind { get; init; }
    public required JsonDocument Arguments { get; init; }
    public string? Reason { get; init; }
    public string? AssistantText { get; init; }
}

public sealed class ApprovalDecision
{
    public required string RequestId { get; init; }
    public required bool Approved { get; init; }
    public string? Comment { get; init; }
}
```

### 5.7 事件

```csharp
// 基类
public abstract record KitEvent
{
    public required string EventType { get; init; }
    public required string RunId { get; init; }
    public required string SessionId { get; init; }
    public required long Sequence { get; init; }
    public required int Attempt { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
}

// 具体事件
public sealed record RunStartedEvent : KitEvent;               // 运行开始
public sealed record ResponseDeltaEvent : KitEvent { public required string TextDelta { get; init; } }
public sealed record ToolInvokedEvent : KitEvent               // 工具调用
{
    public required string CallId { get; init; }
    public required string ToolName { get; init; }
    public required ToolKind ToolKind { get; init; }
    public required string ProviderKey { get; init; }
    public JsonDocument? Arguments { get; init; }
    public JsonDocument? Result { get; init; }
    public bool Succeeded { get; init; }
    public bool RequiresApproval { get; init; }
}
public sealed record ApprovalRequiredEvent : KitEvent { public required PendingApproval Approval { get; init; } }
public sealed record RunCompletedEvent : KitEvent;             // 运行完成
public sealed record RunFailedEvent : KitEvent { public required Failure Failure { get; init; } }
```

## 6. 能力注册协议

### 6.1 Tool Registry

```csharp
public interface IToolRegistry
{
    bool TryResolve(string key, out ToolRegistration? registration);
}

public sealed class ToolRegistration
{
    public required string Key { get; init; }
    public string? DisplayName { get; init; }
    public required Func<IServiceProvider, AIFunction> Factory { get; init; }
    public bool DefaultRequiresApproval { get; init; }
}
```

### 6.2 Skill Registry

```csharp
public interface ISkillRegistry
{
    bool TryResolve(string key, out SkillSetRegistration? registration);
}

public sealed class SkillSetRegistration
{
    public required string Key { get; init; }
    public required IReadOnlyList<string> Paths { get; init; }
    public string? PromptTemplate { get; init; }
    public bool ScriptApproval { get; init; }
    public Func<string, Task<string>>? ScriptRunner { get; init; }
}
```

### 6.3 MCP Registry

```csharp
public interface IMcpServerRegistry
{
    bool TryResolve(string key, out McpServerRegistration? registration);
}

public sealed class McpServerRegistration
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required McpTransportType TransportType { get; init; }
    public McpConnectionDefinition Connection { get; init; } = new();
}

public enum McpTransportType { Stdio, StreamableHttp }

public sealed class McpConnectionDefinition
{
    public string? Command { get; init; }           // stdio
    public IReadOnlyList<string>? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }
    public string? Url { get; init; }               // streamable-http
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}
```

### 6.4 扩展接口

```csharp
// 定义解析器
public interface IDefinitionResolver
{
    Task<AgentKitDefinition> ResolveAsync(string definitionKey, string? version, CancellationToken cancellationToken);
}

// 模型客户端解析器（必须实现）
public interface IModelClientResolver
{
    Task<IChatClient> ResolveAsync(ModelDefinition model, CancellationToken cancellationToken);
}

// 结构化类型解析器
public interface IStructuredTypeResolver
{
    bool TryResolve(string targetTypeName, out Type? type);
}

// 结构化输出修复策略
public interface IStructuredOutputRepairStrategy
{
    Task<string?> TryRepairAsync(string rawOutput, StructuredValidationResult validation, CancellationToken cancellationToken);
}

// 事件发布器
public interface IEventPublisher
{
    Task PublishAsync(KitEvent @event, CancellationToken cancellationToken);
}
```

## 7. 存储协议

### 7.1 Session Store

```csharp
public interface ISessionStore
{
    Task<StoredSession?> GetAsync(string sessionId, CancellationToken cancellationToken);
    Task SaveAsync(StoredSession session, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoredMessage>> LoadMessagesAsync(string sessionId, CancellationToken cancellationToken);
    Task AppendMessagesAsync(string sessionId, IReadOnlyList<StoredMessage> messages, CancellationToken cancellationToken);
}
```

### 7.2 Event Store

```csharp
public interface IEventStore
{
    Task AppendAsync(StoredEvent @event, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoredEvent>> ListAsync(string runId, CancellationToken cancellationToken);
}
```

### 7.3 Approval Store

```csharp
public interface IApprovalStore
{
    Task CreateAsync(StoredApproval approval, CancellationToken cancellationToken);
    Task<StoredApproval?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken);
    Task SaveDecisionAsync(StoredApprovalDecision decision, CancellationToken cancellationToken);
}
```

### 7.4 存储模型

**StoredSession** — 会话快照

```csharp
public sealed class StoredSession
{
    public required string SessionId { get; init; }
    public required string DefinitionKey { get; init; }
    public string? DefinitionVersion { get; init; }
    public JsonElement? AgentSessionData { get; init; }
    public ResumptionContext ResumptionContext { get; init; } = new();
    public long Version { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
```

**StoredMessage** — 消息记录

```csharp
public sealed class StoredMessage
{
    public required string SessionId { get; init; }
    public required long Sequence { get; init; }
    public required MessageRole Role { get; init; }
    public IReadOnlyList<MessageContent>? Contents { get; init; }
    public string? Source { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
```

**StoredEvent** — 事件记录

```csharp
public sealed class StoredEvent
{
    public required string RunId { get; init; }
    public required string SessionId { get; init; }
    public required long Sequence { get; init; }
    public required string EventType { get; init; }
    public required JsonDocument Payload { get; init; }
    public int Attempt { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
}
```

**StoredApproval** — 审批单

```csharp
public sealed class StoredApproval
{
    public required string RequestId { get; init; }
    public required string RunId { get; init; }
    public required string SessionId { get; init; }
    public required ApprovalTargetKind TargetKind { get; init; }
    public required string TargetName { get; init; }
    public required JsonDocument Arguments { get; init; }
    public string? Reason { get; init; }
    public string Status { get; init; } = "pending";
}
```

### 7.5 ChatHistoryProvider 桥接

`ChatHistoryProviderBridge` 将 MAF 的 `ChatHistoryProvider` 桥接到 `ISessionStore`：

- `ProvideChatHistoryAsync`：从 `ISessionStore.LoadMessagesAsync` 加载历史，转换为 `ChatMessage`
- `StoreChatHistoryAsync`：将 MAF 的 Request/Response 消息追加到 `ISessionStore`
- 过滤 ToolApproval 内容（不持久化审批消息）
- PerServiceCall 场景下每次 service call 后自动调用

## 8. 运行时架构

### 8.1 对外主接口

```csharp
public interface IRunner
{
    Task<RunResult> RunAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<KitEvent> StreamAsync(AgentKitDefinition definition, RunRequest request, CancellationToken cancellationToken = default);
}
```

### 8.2 执行分层

```
Runner（校验 → 委托 → 聚合）
  ↓
IExecutionPipeline 接口（Core ↔ Maf 解耦点）
  ↓
MafExecutionPipeline（完整编排）
  ↓
ChatClientAgent（MAF 运行时）
```

**Runner** 职责：
1. 校验 AgentKitDefinition（DefinitionValidator）
2. 校验 RunRequest（RunRequestValidator）
3. 委托 IExecutionPipeline 执行
4. 聚合 PipelineResult 为 RunResult（ResultAggregator）

**MafExecutionPipeline** 职责：
1. 解析模型客户端
2. 构建 ChatHistoryProvider
3. 组装 Agent（Composer.Compose）
4. 创建/恢复会话
5. 检查审批恢复 → 构造恢复消息
6. 构建输入消息（含 MessageContent 转换）
7. 重试循环：执行 → 投影事件 → 校验结构化输出 → 尝试修复 → 重试
8. 派发事件到 IEventStore
9. 提取续跑令牌
10. 序列化并持久化会话
11. 创建审批单（如有）
12. 发射 RunStarted / RunCompleted 事件

### 8.3 重试规则

| 条件 | 行为 |
|------|------|
| `Retry.Enabled == false` | 不重试 |
| 已达 `MaxAttempts` | 不重试 |
| `RetryBeforeVisibleOutputOnly && hasVisibleOutput` | 不重试 |
| 结构化输出失败 + `RetryStructuredFinalization` | 重试 |
| 其他失败 | 不重试 |

延迟：指数退避 `BaseDelay * 2^(attempt-1)`。

## 9. MAF 适配

### 9.1 Composer

从 `AgentKitDefinition` + `IChatClient` 组装 `ChatClientAgent`：

- 映射 ChatOptions（Temperature、TopP、MaxOutputTokens）
- 设置 ChatHistoryProvider
- 配置 PerServiceCallPersistence

### 9.2 Session 适配

- `CreateAsync`：调用 `agent.CreateSessionAsync`
- `RestoreAsync`：调用 `agent.DeserializeSessionAsync`
- `SerializeAsync`：调用 `agent.SerializeSessionAsync`

### 9.3 事件投影

将 `AgentResponse` / `AgentResponseUpdate` 投影为 `KitEvent`：

- `TextDelta` → `ResponseDeltaEvent`
- `FunctionCallContent` → `ToolInvokedEvent`（ProviderKey = "maf"）
- `FunctionResultContent` → 更新对应 ToolInvokedEvent 的 Result

### 9.4 审批恢复

```csharp
// 从 PendingApproval + ApprovalDecision 构造 MAF 恢复消息
var functionCall = new FunctionCallContent(callId, name, arguments);
var approvalRequest = new ToolApprovalRequestContent(requestId, functionCall);
var approvalResponse = approvalRequest.CreateResponse(approved, comment);
var resumeMessage = new ChatMessage(ChatRole.User, [approvalRequest, approvalResponse]);
// prepend 到消息列表
```

### 9.5 续跑令牌

- 入：`RunRequest.ContinuationToken` → `ContinuationTokenConverter.ToMaf()` → `AgentRunOptions.ContinuationToken`
- 出：`AgentResponse.ContinuationToken` → `ContinuationTokenConverter.FromMaf()` → `PipelineResult.ContinuationToken`

### 9.6 结构化输出

- 配置 `ChatOptions.ResponseFormat = ChatResponseFormatJson(schema)`
- 运行后调用 `StructuredOutputValidator.Validate(output, schema)` 校验
- 校验失败时尝试 `IStructuredOutputRepairStrategy.TryRepairAsync`
- 支持通过 `IStructuredTypeResolver` 反序列化为目标类型

## 10. DI 注册

```csharp
// 一站式
builder.Services.AddAgentKit();

// 等价于
builder.Services.AddAgentKitCore();      // Runner, IRunner
builder.Services.AddAgentKitMaf();       // Composer, SessionAdapter, EventProjector, IExecutionPipeline
builder.Services.AddAgentKitInMemoryStorage(); // ISessionStore, IEventStore, IApprovalStore

// 必须额外注册
builder.Services.AddSingleton<IModelClientResolver, MyResolver>();

// 可选注册
builder.Services.AddSingleton<IStructuredTypeResolver, StructuredTypeResolver>();
builder.Services.AddSingleton<IStructuredOutputRepairStrategy, MyRepairStrategy>();
builder.Services.AddSingleton<IEventPublisher, MyEventPublisher>();
builder.Services.AddSingleton<IToolRegistry>(toolRegistry);
builder.Services.AddSingleton<ISkillRegistry>(skillRegistry);
builder.Services.AddSingleton<IMcpServerRegistry>(mcpRegistry);
```
