using FreshRescue.Entities.Enums;
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

    /// <summary>
    /// <c>UPDATE Orders SET Status = Cancelled … WHERE Id = @id AND Status = @expectedStatus</c>, chạy
    /// ngay. Chỉ khi trả <c>true</c> mới được hoàn tồn kho — chốt chặn chống hoàn hai lần làm kho
    /// phình lên (02-DOMAIN-MODEL.md §6.4). Phải gọi trong giao dịch.
    /// </summary>
    Task<bool> TryCancelAsync(
        Guid orderId,
        OrderStatus expectedStatus,
        CancelledBy cancelledBy,
        string? cancelReason,
        RefundStatus refundStatus,
        DateTime cancelledAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// <c>UPDATE Orders SET Status = NoShow … WHERE Id = @id AND Status = Paid</c>, chạy ngay
    /// không chờ <c>SaveChangesAsync</c>. Trả <c>false</c> khi không khớp dòng nào — đơn đã được
    /// nhận hoặc đổi trạng thái trước đó (02-DOMAIN-MODEL.md §6.4, BR-19).
    /// </summary>
    Task<bool> TryMarkNoShowAsync(Guid orderId, DateTime noShowAt, CancellationToken cancellationToken);
}
