using AgentKit.Storage.Models;

namespace AgentKit.Storage;

/// <summary>审批存储，管理审批单的创建、查询与决策。</summary>
public interface IApprovalStore
{
    /// <summary>创建审批单。</summary>
    /// <param name="approval">审批单。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task CreateAsync(StoredApproval approval, CancellationToken cancellationToken);

    /// <summary>按请求 ID 查询审批单。</summary>
    /// <param name="requestId">审批请求 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>审批单，不存在时返回 null。</returns>
    Task<StoredApproval?> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken);

    /// <summary>保存审批决策。</summary>
    /// <param name="decision">审批决策。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveDecisionAsync(StoredApprovalDecision decision, CancellationToken cancellationToken);
}
