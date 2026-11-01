using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess.Repositories;

/// <summary>
/// ExecuteUpdateAsync executes conditional SQL UPDATE and returns affected row count.
/// These writes are immediately executed; approval wraps them in a UoW transaction.
/// </summary>
public sealed class DisputeWorkflowRepository : IDisputeWorkflowRepository
{
    private readonly FreshRescueDbContext _context;

    public DisputeWorkflowRepository(FreshRescueDbContext context) => _context = context;

    public Task<int> TryResolveAsync(
        Guid disputeId, DisputeStatus next, Guid adminUserId,
        string? adminNote, DateTime utcNow, CancellationToken ct)
        => _context.Disputes
            .Where(d => d.Id == disputeId && d.Status == DisputeStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, next)
                .SetProperty(d => d.ResolvedByUserId, adminUserId)
                .SetProperty(d => d.ResolvedAt, utcNow)
                .SetProperty(d => d.AdminNote, adminNote)
                .SetProperty(d => d.UpdatedAt, utcNow), ct);

    public Task<int> TryRefundOrderAsync(Guid orderId, DateTime utcNow, CancellationToken ct)
        => _context.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.Redeemed
                && o.RefundStatus == RefundStatus.None)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.RefundStatus, RefundStatus.Refunded)
                .SetProperty(o => o.UpdatedAt, utcNow), ct);

    public Task<int> TrySetMerchantNoteAsync(
        Guid disputeId, string merchantNote, DateTime utcNow, CancellationToken ct)
        => _context.Disputes
            .Where(d => d.Id == disputeId && d.Status == DisputeStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.MerchantNote, merchantNote)
                .SetProperty(d => d.UpdatedAt, utcNow), ct);
}
