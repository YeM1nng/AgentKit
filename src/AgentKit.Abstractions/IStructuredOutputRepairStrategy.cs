using AgentKit.Protocol.Results;

namespace AgentKit.Abstractions;

/// <summary>结构化输出校验失败后尝试修复的策略。</summary>
public interface IStructuredOutputRepairStrategy
{
    /// <summary>尝试修复输出。返回修复后的文本，无法修复时返回 null。</summary>
    /// <param name="rawOutput">原始输出文本。</param>
    /// <param name="validation">校验结果。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>修复后的文本，无法修复时返回 null。</returns>
    Task<string?> TryRepairAsync(string rawOutput, StructuredValidationResult validation, CancellationToken cancellationToken);
}
