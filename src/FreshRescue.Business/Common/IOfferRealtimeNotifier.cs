namespace FreshRescue.Business.Common;

/// <summary>
/// Gửi thông báo thời gian thực qua SignalR OfferHub (03-API-CONTRACT.md §10).
/// Tầng API cài đặt interface này bằng IHubContext&lt;OfferHub&gt;.
/// </summary>
public interface IOfferRealtimeNotifier
{
    /// <summary>
    /// Bắn sự kiện OfferQuantityChanged tới các client theo dõi đợt bán (FR-X06).
    /// </summary>
    Task NotifyOfferQuantityChangedAsync(Guid offerId, Guid saleBatchId, int remainingQuantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bắn sự kiện SaleBatchActivated tới các client khi đợt bán mở (FR-X01).
    /// </summary>
    Task NotifySaleBatchActivatedAsync(Guid saleBatchId, Guid storeId, string storeName, int offerCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bắn sự kiện SaleBatchClosed tới các client khi đợt bán đóng (FR-X02).
    /// </summary>
    Task NotifySaleBatchClosedAsync(Guid saleBatchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bắn sự kiện OrderStatusChanged riêng cho khách hàng chủ đơn.
    /// </summary>
    Task NotifyOrderStatusChangedAsync(Guid customerUserId, Guid orderId, string status, CancellationToken cancellationToken = default);
}
