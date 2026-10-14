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

    /// <summary>Khách huỷ đơn (FR-C10, BR-13).</summary>
    Task<OrderDto> CancelByCustomerAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Cửa hàng huỷ đơn đã thanh toán do sự cố, hoàn 100% và báo khách (FR-S07, BR-16).</summary>
    Task<OrderDto> CancelByStoreAsync(Guid orderId, CancelOrderByStoreRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Phần đơn hàng của việc huỷ cả đợt (BR-23): huỷ mọi đơn <c>PendingPayment</c> và <c>Paid</c>
    /// theo đúng luồng BR-16. <c>SaleBatchService</c> gọi hàm này <b>bên trong giao dịch của mình</b>,
    /// cùng giao dịch với việc đổi <c>SaleBatch.Status</c> — hàm không tự mở giao dịch.
    /// Ném <c>BATCH_HAS_REDEEMED_ORDERS</c> nếu đợt đã có đơn trao hàng. Trả về số đơn đã huỷ.
    /// </summary>
    Task<int> CancelBatchOrdersAsync(Guid saleBatchId, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// FR-X03: huỷ đơn quá hạn thanh toán và trả tồn kho. BackgroundService gọi mỗi phút
    /// (07-WORK-BREAKDOWN.md §12.2). Chạy lại nhiều lần trên cùng đơn vẫn an toàn. Trả về số đơn đã huỷ.
    /// </summary>
    Task<int> ExpireOverdueOrdersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// FR-X04: Quét đơn đã thanh toán (Paid) mà khách không tới nhận sau PickupEndTime,
    /// chuyển thành NoShow (BR-19, không hoàn tiền). Trả về số đơn đã đánh dấu NoShow.
    /// </summary>
    Task<int> ProcessNoShowsAsync(CancellationToken cancellationToken, Guid? saleBatchId = null);
}
