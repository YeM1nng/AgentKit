using AgentKit.Protocol.Definitions;

namespace AgentKit.Core;

/// <summary>安全重试，仅在未发出用户可见文本时允许自动重试。</summary>
public static class RetryCoordinator
{
    /// <summary>判断是否应重试。</summary>
    /// <param name="retryPolicy">重试策略。</param>
    /// <param name="currentAttempt">当前尝试次数（从 1 开始）。</param>
    /// <param name="hasVisibleOutput">是否已发出用户可见输出。</param>
    /// <returns>是否应重试。</returns>
    public static bool ShouldRetry(RetryPolicyDefinition retryPolicy, int currentAttempt, bool hasVisibleOutput)
    {
        if (!retryPolicy.Enabled)
            return false;

        if (currentAttempt >= retryPolicy.MaxAttempts)
            return false;

        if (retryPolicy.RetryBeforeVisibleOutputOnly && hasVisibleOutput)
            return false;

        return true;
    }

    /// <summary>计算重试延迟（指数退避）。</summary>
    /// <param name="baseDelay">基础延迟。</param>
    /// <param name="attempt">当前尝试次数（从 1 开始）。</param>
    /// <returns>延迟时间。</returns>
    public static TimeSpan CalculateDelay(TimeSpan baseDelay, int attempt)
    {
        return TimeSpan.FromTicks(baseDelay.Ticks * (1L << (attempt - 1)));
    }
}
