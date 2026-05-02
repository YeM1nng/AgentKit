using System.Text.Json;

using AgentKit.Protocol.Definitions;

using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Structured;

/// <summary>协调结构化输出的路径选择与配置。</summary>
/// <remarks>
/// 根据定义配置 ChatOptions 的 ResponseFormat。
/// 当模型不支持 Tools + Structured Output 并存时，由 Core 层处理二段式 fallback。
/// </remarks>
public static class StructuredOutputCoordinator
{
    /// <summary>根据定义配置 ChatOptions 的结构化输出格式。</summary>
    /// <param name="chatOptions">ChatOptions 实例。</param>
    /// <param name="definition">结构化输出定义。</param>
    public static void Configure(ChatOptions chatOptions, StructuredOutputDefinition? definition)
    {
        if (definition?.Schema is null)
            return;

        chatOptions.ResponseFormat = new ChatResponseFormatJson(
            definition.Schema.RootElement.Clone(),
            definition.Name,
            definition.Version);
    }

    /// <summary>判断是否需要二段式 fallback。</summary>
    /// <param name="definition">结构化输出定义。</param>
    /// <param name="execution">执行策略定义。</param>
    /// <param name="hasTools">是否配置了工具。</param>
    /// <returns>是否需要二段式执行。</returns>
    public static bool RequiresTwoPhaseExecution(
        StructuredOutputDefinition? definition,
        ExecutionDefinition execution,
        bool hasTools)
    {
        if (definition is null)
            return false;

        if (!hasTools)
            return false;

        // 显式配置
        if (execution.ModelSupportsToolsWithStructuredOutput == true)
            return false;

        // 默认假设需要二段式
        return true;
    }
}
