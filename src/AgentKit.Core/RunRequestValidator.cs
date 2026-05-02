using AgentKit.Protocol.Requests;

namespace AgentKit.Core;

/// <summary>校验 RunRequest 的合法性。</summary>
public static class RunRequestValidator
{
    /// <summary>校验请求，返回校验错误列表。空列表表示通过。</summary>
    /// <param name="request">运行请求。</param>
    /// <returns>校验错误列表。</returns>
    public static IReadOnlyList<string> Validate(RunRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Input)
            && (request.Messages is null || request.Messages.Count == 0))
        {
            errors.Add("输入内容和消息列表不能同时为空。");
        }

        return errors;
    }
}
