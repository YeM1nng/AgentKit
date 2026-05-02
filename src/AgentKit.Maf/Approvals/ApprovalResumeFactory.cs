using System.Text.Json;

using AgentKit.Protocol.Sessions;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentKit.Maf.Approvals;

/// <summary>从 PendingApproval 构造 MAF 审批恢复消息。</summary>
public static class ApprovalResumeFactory
{
    /// <summary>构造审批恢复消息，用于继续被审批中断的运行。</summary>
    /// <param name="pending">待审批项。</param>
    /// <param name="decision">审批决策。</param>
    /// <returns>包含审批请求与响应的恢复消息。</returns>
    public static ChatMessage CreateResumeMessage(PendingApproval pending, ApprovalDecision decision)
    {
        var arguments = DeserializeArguments(pending.Arguments);
        var functionCall = new FunctionCallContent(pending.CallId, pending.TargetName, arguments);
        var approvalRequest = new ToolApprovalRequestContent(pending.RequestId, functionCall);
        var approvalResponse = approvalRequest.CreateResponse(decision.Approved, decision.Comment ?? string.Empty);

        return new ChatMessage(ChatRole.User, [approvalRequest, approvalResponse]);
    }

    private static IDictionary<string, object?>? DeserializeArguments(JsonDocument? document)
    {
        if (document is null)
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(document.RootElement.GetRawText());
    }
}
