using FreshRescue.DataAccess;
using FreshRescue.DataAccess.Repositories;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Tests.Ordering;

/// <summary>
/// InMemory không chạy được ExecuteUpdate. Hai bản này mô phỏng đúng điều kiện WHERE và chặn trần
/// của repository thật; phần SQL thật được kiểm bằng chạy trên SQL Server.
/// </summary>
internal sealed class InMemoryOrderRepository(FreshRescueDbContext context)
    : GenericRepository<Order>(context), IOrderRepository
{
    /// <summary>Chen một thay đổi vào giữa lúc service đọc và lúc ghi.</summary>
    public Func<Task>? BeforeUpdate { get; set; }

    public async Task<bool> TryMarkRedeemedAsync(Guid orderId, DateTime redeemedAt, CancellationToken cancellationToken)
    {
        var order = await FindForUpdateAsync(orderId, OrderStatus.Paid, cancellationToken);
        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.Redeemed;
        order.RedeemedAt = redeemedAt;
        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryCancelAsync(
        Guid orderId,
        OrderStatus expectedStatus,
        CancelledBy cancelledBy,
        string? cancelReason,
        RefundStatus refundStatus,
        DateTime cancelledAt,
        CancellationToken cancellationToken)
    {
        var order = await FindForUpdateAsync(orderId, expectedStatus, cancellationToken);
        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = cancelledBy;
        order.CancelReason = cancelReason;
        order.RefundStatus = refundStatus;
        order.CancelledAt = cancelledAt;
        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Order?> FindForUpdateAsync(Guid orderId, OrderStatus expectedStatus, CancellationToken cancellationToken)
    {
        if (BeforeUpdate is not null)
        {
            await BeforeUpdate();
        }

        return await Set.SingleOrDefaultAsync(o => o.Id == orderId && o.Status == expectedStatus, cancellationToken);
    }
}

internal sealed class InMemoryOfferRepository(FreshRescueDbContext context)
    : GenericRepository<Offer>(context), IOfferRepository
{
    public Task<List<Offer>> GetForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        => Set.Where(o => ids.Contains(o.Id)).ToListAsync(cancellationToken);

    public async Task RestockAsync(IReadOnlyDictionary<Guid, int> quantities, CancellationToken cancellationToken)
    {
        foreach (var (offerId, quantity) in quantities)
        {
            var offer = await Set.IgnoreQueryFilters().SingleAsync(o => o.Id == offerId, cancellationToken);
            offer.RemainingQuantity = Math.Min(offer.RemainingQuantity + quantity, offer.TotalQuantity);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAdjustTotalAsync(Guid offerId, int newTotalQuantity, CancellationToken cancellationToken)
    {
        var offer = await Set.SingleOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        if (offer is null || offer.RemainingQuantity + (newTotalQuantity - offer.TotalQuantity) < 0)
        {
            return false;
        }

        offer.RemainingQuantity += newTotalQuantity - offer.TotalQuantity;
        offer.TotalQuantity = newTotalQuantity;
        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
