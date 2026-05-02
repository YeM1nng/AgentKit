# AgentKit 项目约定

## 项目定位

AgentKit 是基于 Microsoft Agent Framework (MAF) 的运行时能力库，以 NuGet 包形式分发。
遵循 MAF-first 原则，直接复用 MAF 官方能力，不做重复实现。

## 目标框架

- TargetFramework: `net10.0`
- 命名空间: `AgentKit.*`
- 语言: C# 14

## 命名约定

### 核心原则

1. **协议类型**（`AgentKit.Protocol`）：保留 `AgentKit` 前缀。跨项目使用的公共契约，前缀避免命名冲突。
2. **接口类型**：去掉 `AgentKit` 前缀。命名空间已提供上下文。例如 `ISessionStore` 而非 `IAgentKitSessionStore`。
3. **实现类型**：去掉 `AgentKit`/`Maf` 前缀。命名空间已隔离。例如 `Composer` 而非 `MafAgentComposer`。
4. **DI 扩展方法**：保留 `AgentKit` 前缀，因为它们在 `IServiceCollection` 上扩展。例如 `AddAgentKit()`。
5. **文件名与类型名一致**，非必要不添加项目前缀。

### 示例

```
AgentKit.Protocol.AgentKitDefinition   ← 保留前缀（公共契约）
AgentKit.Abstractions.IRunner          ← 去掉前缀
AgentKit.Core.Runner                   ← 去掉前缀
AgentKit.Maf.Composer                  ← 去掉前缀
AgentKit.Storage.Abstractions.ISessionStore ← 去掉前缀
```

## 注释规范

- 所有 `public` / `internal` 类、接口、record、enum 必须加 `/// <summary>` XML 文档注释。
- 所有 `public` / `internal` 方法必须加 `/// <summary>` + `/// <param>` + `/// <returns>`。
- 所有 `public` 属性必须加 `/// <summary>`。
- 枚举成员必须逐个注释。
- 注释使用**中文**，简洁描述用途，不写实现细节。

## 代码精简原则

- 不写多余兜底代码（不加不会发生的 null 检查、不加不会触发的 fallback）。
- 不写多余的 try/catch（信任内部调用链）。
- 系统边界（外部输入、存储读取）才做校验。
- 使用 C# 14 最新语法特性（`required`、`init`、record、pattern matching 等）。
- 默认不写注释，XML 文档注释除外。

## 项目结构

```
src/
  AgentKit.Protocol/                  # 协议层（纯类型定义，零外部依赖）
  AgentKit.Abstractions/             # 抽象层（扩展点接口）
  AgentKit.Capabilities/             # 能力注册（Tools/Skills/MCP）
  AgentKit.Storage.Abstractions/     # 存储协议（零外部依赖）
  AgentKit.Storage.InMemory/         # 内存存储实现
  AgentKit.Core/                     # 运行编排（协调层，不依赖 MAF）
  AgentKit.Maf/                      # MAF 适配层
  AgentKit.Extensions.DependencyInjection/  # DI 扩展（内置 OpenAI provider）
tests/
  AgentKit.Protocol.Tests/
  AgentKit.Storage.InMemory.Tests/
  AgentKit.Maf.Tests/
  AgentKit.Core.Tests/
```

## 项目引用关系

```
AgentKit.Protocol          → 无引用（纯协议）
AgentKit.Abstractions      → AgentKit.Protocol
AgentKit.Capabilities      → AgentKit.Protocol
AgentKit.Storage.Abstractions → 无引用（纯协议）
AgentKit.Storage.InMemory  → AgentKit.Storage.Abstractions
AgentKit.Core              → Protocol, Abstractions, Capabilities, Storage.Abstractions
AgentKit.Maf               → Protocol, Abstractions, Capabilities, Storage.Abstractions, MAF NuGet
AgentKit.Extensions.DependencyInjection → Core, Maf, Capabilities, Storage.InMemory, OpenAI NuGet, Options NuGet
```

关键约束：**Core 不直接依赖 MAF**。

## 沟通语言

- 文档、注释、代码内字符串：**中文**
- 类型名、方法名、变量名：**英文**
