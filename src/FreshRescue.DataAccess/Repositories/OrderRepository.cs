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
}
