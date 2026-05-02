using AgentKit.Protocol.Definitions;

using Xunit;

namespace AgentKit.Core.Tests;

/// <summary>重试协调器测试。</summary>
public class RetryCoordinatorTests
{
    [Fact]
    public void ShouldRetry_Disabled_ReturnsFalse()
    {
        var policy = new RetryPolicyDefinition { Enabled = false, MaxAttempts = 3 };
        Assert.False(RetryCoordinator.ShouldRetry(policy, 1, false));
    }

    [Fact]
    public void ShouldRetry_MaxAttemptsReached_ReturnsFalse()
    {
        var policy = new RetryPolicyDefinition { Enabled = true, MaxAttempts = 3 };
        Assert.False(RetryCoordinator.ShouldRetry(policy, 3, false));
    }

    [Fact]
    public void ShouldRetry_WithinLimit_ReturnsTrue()
    {
        var policy = new RetryPolicyDefinition { Enabled = true, MaxAttempts = 3 };
        Assert.True(RetryCoordinator.ShouldRetry(policy, 1, false));
        Assert.True(RetryCoordinator.ShouldRetry(policy, 2, false));
    }

    [Fact]
    public void ShouldRetry_VisibleOutput_BeforeVisibleOnly_ReturnsFalse()
    {
        var policy = new RetryPolicyDefinition
        {
            Enabled = true,
            MaxAttempts = 3,
            RetryBeforeVisibleOutputOnly = true,
        };

        Assert.False(RetryCoordinator.ShouldRetry(policy, 1, hasVisibleOutput: true));
    }

    [Fact]
    public void ShouldRetry_VisibleOutput_NotBeforeVisibleOnly_ReturnsTrue()
    {
        var policy = new RetryPolicyDefinition
        {
            Enabled = true,
            MaxAttempts = 3,
            RetryBeforeVisibleOutputOnly = false,
        };

        Assert.True(RetryCoordinator.ShouldRetry(policy, 1, hasVisibleOutput: true));
    }

    [Fact]
    public void CalculateDelay_ExponentialBackoff_Succeeds()
    {
        var baseDelay = TimeSpan.FromSeconds(1);

        Assert.Equal(TimeSpan.FromSeconds(1), RetryCoordinator.CalculateDelay(baseDelay, 1));
        Assert.Equal(TimeSpan.FromSeconds(2), RetryCoordinator.CalculateDelay(baseDelay, 2));
        Assert.Equal(TimeSpan.FromSeconds(4), RetryCoordinator.CalculateDelay(baseDelay, 3));
        Assert.Equal(TimeSpan.FromSeconds(8), RetryCoordinator.CalculateDelay(baseDelay, 4));
    }
}
