using AgentKit.Protocol.Requests;

namespace AgentKit.Core;

/// <summary>协调会话的创建、恢复与持久化。</summary>
public static class SessionCoordinator
{
    /// <summary>从请求中提取会话 ID，若无则生成新 ID。</summary>
    /// <param name="request">运行请求。</param>
    /// <returns>会话 ID。</returns>
    public static string ResolveSessionId(RunRequest request)
    {
        return request.Session?.SessionId ?? Guid.NewGuid().ToString("N");
    }

    /// <summary>构造运行 ID。</summary>
    /// <returns>唯一的运行 ID。</returns>
    public static string CreateRunId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
