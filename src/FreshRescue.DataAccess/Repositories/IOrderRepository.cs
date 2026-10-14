using FreshRescue.Entities.Ordering;

namespace FreshRescue.DataAccess.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    /// <summary>
    /// <c>UPDATE Orders SET Status = Redeemed … WHERE Id = @id AND Status = Paid</c>, chạy ngay
    /// không chờ <c>SaveChangesAsync</c>. Trả <c>false</c> khi không khớp dòng nào — đơn đã được
    /// quét hoặc vừa đổi trạng thái (BR-09, 02-DOMAIN-MODEL.md §6.5). Phải gọi trong giao dịch.
    /// </summary>
    Task<bool> TryMarkRedeemedAsync(Guid orderId, DateTime redeemedAt, CancellationToken cancellationToken);
}
