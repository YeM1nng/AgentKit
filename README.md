# AgentKit

基于 Microsoft Agent Framework (MAF) 的 Agent 运行时库，提供统一的协议层、编排层和存储层。

## 项目结构

```
src/
  AgentKit.Protocol/                    # 协议层：纯类型定义，零外部依赖
  AgentKit.Abstractions/               # 抽象层：扩展点接口
  AgentKit.Capabilities/               # 能力注册：Tools / Skills / MCP
  AgentKit.Storage.Abstractions/       # 存储协议（零外部依赖）
  AgentKit.Storage.InMemory/           # 内存存储实现
  AgentKit.Core/                       # 编排层：Runner / Validator（不依赖 MAF）
  AgentKit.Maf/                        # MAF 适配层：Composer / SessionAdapter / EventProjector
  AgentKit.Extensions.DependencyInjection/  # DI 扩展（内置 OpenAI provider）
```

## 快速开始

```csharp
// 1. 注册（内置 OpenAI provider + InMemory 存储）
builder.Services.AddAgentKit();

// 2. 运行 — 通过 CredentialKey 传入 API Key
var definition = new AgentKitDefinition
{
    Key = "assistant",
    Name = "Assistant",
    Model = new ModelDefinition
    {
        Provider = "openai",
        ModelId = "gpt-4o",
        CredentialKey = "sk-...",  // API Key（运行时动态传入）
    },
    SystemPrompt = "你是一个有帮助的助手",
};
var result = await runner.RunAsync(definition, new RunRequest { Input = "Hello" });
```

## 自定义 Provider

```csharp
// 注册自定义 provider 工厂（在 AddAgentKit 之后调用）
builder.Services.AddModelProvider("azure-openai", (model, ct) =>
{
    var credential = new Azure.AzureKeyCredential(/* 从配置获取 */);
    var client = new Azure.AI.OpenAI.AzureOpenAIClient(
        new Uri(model.Endpoint!), credential);
    return Task.FromResult<IChatClient>(
        client.GetChatClient(model.ModelId).AsIChatClient());
});

builder.Services.AddModelProvider("custom", (model, ct) =>
{
    // 支持自定义 ModelDefinition 子类型
    if (model is MyModelDefinition custom)
        // 使用 custom.ExtraParam ...
    return /* ... */;
});

// 运行时通过 Provider 字段路由
var definition = new AgentKitDefinition
{
    Model = new ModelDefinition { Provider = "azure-openai", ModelId = "gpt-4o", Endpoint = "https://..." },
};
```

## 示例 1：多轮对话（会话管理）

```csharp
public class MultiTurnChat(IRunner runner)
{
    private readonly Dictionary<string, SessionState> _sessions = new();

    public async Task<string> ChatAsync(string sessionId, string input)
    {
        var definition = new AgentKitDefinition
        {
            Key = "chat",
            Name = "Chat",
            Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o", CredentialKey = "sk-..." },
        };

        _sessions.TryGetValue(sessionId, out var session);

        var result = await runner.RunAsync(definition, new RunRequest
        {
            Input = input,
            Session = session,
        });

        if (result.SessionState is not null)
            _sessions[sessionId] = result.SessionState;

        return result.FinalText ?? "";
    }
}
```

## 示例 2：审批流程

```csharp
var definition = new AgentKitDefinition
{
    Key = "file-manager",
    Name = "File Manager",
    Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o", CredentialKey = "sk-..." },
    Tools =
    [
        new ToolReference { Key = "delete-file", RequiresApproval = true, ApprovalReason = "删除不可逆" },
    ],
};

// 第一次运行：Agent 调用 delete-file，触发审批
var result1 = await runner.RunAsync(definition, new RunRequest { Input = "删除 /tmp/old.log" });

if (result1.RunState == RunState.CompletedWithApproval)
{
    var pa = result1.PendingApproval!;
    Console.WriteLine($"需要审批: {pa.TargetName}, 原因: {pa.Reason}");

    var result2 = await runner.RunAsync(definition, new RunRequest
    {
        Session = result1.SessionState,
        ApprovalDecision = new ApprovalDecision
        {
            RequestId = pa.RequestId,
            Approved = true,
            Comment = "确认删除",
        },
    });
}
```

## 示例 3：结构化输出

```csharp
var schema = JsonDocument.Parse("""
{
    "type": "object",
    "properties": {
        "name": { "type": "string" },
        "age": { "type": "number" }
    },
    "required": ["name", "age"]
}
""");

var definition = new AgentKitDefinition
{
    Key = "profile",
    Name = "Profile Generator",
    Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o", CredentialKey = "sk-..." },
    StructuredOutput = new StructuredOutputDefinition
    {
        Name = "UserProfile",
        Schema = schema,
        StrictValidation = true,
    },
};

var result = await runner.RunAsync(definition, new RunRequest
{
    Input = "生成一个 25 岁的开发者",
});

if (result.RunState == RunState.Completed)
{
    var name = result.StructuredPayload!.RootElement.GetProperty("name").GetString();
}
```

## 示例 3b：结构化输出自动修复

```csharp
var definition = new AgentKitDefinition
{
    Key = "profile",
    Name = "Profile Generator",
    Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o", CredentialKey = "sk-..." },
    StructuredOutput = new StructuredOutputDefinition
    {
        Name = "UserProfile",
        Schema = schema,
        StrictValidation = true,
        AutoRepair = true, // 校验失败时自动发送修复请求
        // RepairPrompt = "自定义修复提示词...", // 可选，覆盖默认提示词
    },
};

// 修复流程：
// 1. AI 输出 → Schema 校验
// 2. 校验失败 → 发送修复请求（schema + 原始输出 + 错误信息）
// 3. 修复成功 → 返回修复后的结果
// 4. 修复失败 → 降级到重试策略
```

## 示例 4：流式运行

```csharp
await foreach (var ev in runner.StreamAsync(definition, request, ct))
{
    switch (ev)
    {
        case ResponseDeltaEvent delta:
            Console.Write(delta.TextDelta);
            break;
        case ToolInvokedEvent tool:
            Console.WriteLine($"\n[工具: {tool.ToolName}]");
            break;
        case RunCompletedEvent:
            Console.WriteLine("\n[完成]");
            break;
    }
}
```

## 示例 5：自动重试

```csharp
var definition = new AgentKitDefinition
{
    Key = "reliable",
    Name = "Reliable Agent",
    Model = new ModelDefinition { Provider = "openai", ModelId = "gpt-4o", CredentialKey = "sk-..." },
    Execution = new ExecutionDefinition
    {
        Retry = new RetryPolicyDefinition
        {
            Enabled = true,
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromSeconds(1),
            RetryBeforeVisibleOutputOnly = true,
            RetryStructuredFinalization = true,
        },
    },
};

var result = await runner.RunAsync(definition, request);
Console.WriteLine($"使用了 {result.AttemptsUsed} 次尝试");
```

## 示例 6：自定义存储

```csharp
builder.Services.AddSingleton<ISessionStore, PostgresSessionStore>();
builder.Services.AddSingleton<IEventStore, RedisEventStore>();
builder.Services.AddSingleton<IApprovalStore, PostgresApprovalStore>();
```

## 示例 7：完整应用配置

```csharp
var builder = WebApplication.CreateBuilder(args);

// AgentKit（内置 OpenAI + InMemory 存储）
builder.Services.AddAgentKit();

// 自定义 provider（可选）
builder.Services.AddModelProvider("azure", (model, ct) =>
{
    var client = new Azure.AI.OpenAI.AzureOpenAIClient(
        new Uri(model.Endpoint!), new Azure.AzureKeyCredential("..."));
    return Task.FromResult<IChatClient>(
        client.GetChatClient(model.ModelId).AsIChatClient());
});

// 替换存储（可选）
builder.Services.AddSingleton<ISessionStore, PostgresSessionStore>();
builder.Services.AddSingleton<IEventStore, RedisEventStore>();
builder.Services.AddSingleton<IApprovalStore, PostgresApprovalStore>();

// 可选扩展
builder.Services.AddSingleton<IStructuredTypeResolver, StructuredTypeResolver>();
```

## 事件类型

| 事件 | 说明 | 关键属性 |
|------|------|----------|
| RunStartedEvent | 运行开始 | RunId, SessionId |
| ResponseDeltaEvent | 增量文本 | TextDelta |
| ToolInvokedEvent | 工具调用 | ToolName, CallId, Arguments |
| ApprovalRequiredEvent | 审批请求 | Approval |
| RunCompletedEvent | 运行完成 | - |
| RunFailedEvent | 运行失败 | Failure |

## 运行状态

| 状态 | 说明 |
|------|------|
| Completed | 正常完成 |
| CompletedWithToolCalls | 调用了工具 |
| CompletedWithApproval | 等待审批 |
| CompletedWithContinuation | 需要续跑 |
| Failed | 运行失败 |

## 扩展点

| 接口 | 说明 |
|------|------|
| ISessionStore | 会话存储，默认 InMemory |
| IEventStore | 事件持久化 |
| IApprovalStore | 审批单持久化 |
| IStructuredTypeResolver | 结构化类型解析 |
| IStructuredOutputRepairStrategy | 输出修复 |
