using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(FreshRescueDbContext context)
        : base(context)
    {
    }

    public async Task<bool> TryMarkRedeemedAsync(Guid orderId, DateTime redeemedAt, CancellationToken cancellationToken)
    {
        // ExecuteUpdate đi thẳng xuống CSDL, bỏ qua SaveChanges nên phải tự ghi UpdatedAt.
        var affected = await Set
            .Where(o => o.Id == orderId && o.Status == OrderStatus.Paid)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(o => o.Status, OrderStatus.Redeemed)
                    .SetProperty(o => o.RedeemedAt, redeemedAt)
                    .SetProperty(o => o.UpdatedAt, redeemedAt),
                cancellationToken);
        return affected == 1;
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
        var affected = await Set
            .Where(o => o.Id == orderId && o.Status == expectedStatus)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(o => o.Status, OrderStatus.Cancelled)
                    .SetProperty(o => o.CancelledBy, cancelledBy)
                    .SetProperty(o => o.CancelReason, cancelReason)
                    .SetProperty(o => o.RefundStatus, refundStatus)
                    .SetProperty(o => o.CancelledAt, cancelledAt)
                    .SetProperty(o => o.UpdatedAt, cancelledAt),
                cancellationToken);
        return affected == 1;
    }

    public async Task<bool> TryMarkNoShowAsync(Guid orderId, DateTime noShowAt, CancellationToken cancellationToken)
    {
        var affected = await Set
            .Where(o => o.Id == orderId && o.Status == OrderStatus.Paid)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(o => o.Status, OrderStatus.NoShow)
                    .SetProperty(o => o.RefundStatus, RefundStatus.None)
                    .SetProperty(o => o.UpdatedAt, noShowAt),
                cancellationToken);
        return affected == 1;
    }
}
