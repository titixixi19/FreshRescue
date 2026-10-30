using FreshRescue.Entities.Enums;

namespace FreshRescue.DataAccess.Repositories;

/// <summary>
/// Những cập nhật có điều kiện phải chạy atomic ở SQL Server, không đọc rồi ghi.
/// </summary>
public interface IDisputeWorkflowRepository
{
    Task<int> TryResolveAsync(
        Guid disputeId, DisputeStatus next, Guid adminUserId,
        string? adminNote, DateTime utcNow, CancellationToken ct);

    Task<int> TryRefundOrderAsync(Guid orderId, DateTime utcNow, CancellationToken ct);

    Task<int> TrySetMerchantNoteAsync(Guid disputeId, string merchantNote, DateTime utcNow, CancellationToken ct);
}
