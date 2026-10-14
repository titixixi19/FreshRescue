using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering;

public interface IOrderService
{
    /// <summary>Đặt hàng và giữ chỗ tồn kho ngay (BR-04).</summary>
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    /// <summary>Thanh toán giả lập (OQ-17). Thành công thì cấp vé QR (FR-C06, FR-C07).</summary>
    Task<OrderPaymentDto> PayAsync(Guid orderId, PayOrderRequest request, CancellationToken cancellationToken);

    Task<QrTicketDto> GetQrTicketAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Nhân viên quét QR hoặc nhập mã đơn để trao hàng (FR-S05, FR-S06). Nằm ở đây chứ không ở
    /// service riêng vì chỉ OrderService được ghi <c>Order.Status</c> (07-WORK-BREAKDOWN.md §12.2).
    /// </summary>
    Task<RedemptionDto> RedeemAsync(RedeemOrderRequest request, CancellationToken cancellationToken);
}
