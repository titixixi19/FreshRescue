using FreshRescue.Business.Common;
using Microsoft.AspNetCore.SignalR;

namespace FreshRescue.API.Hubs;

public class OfferRealtimeNotifier : IOfferRealtimeNotifier
{
    private readonly IHubContext<OfferHub> _hubContext;
    private readonly ILogger<OfferRealtimeNotifier> _logger;

    public OfferRealtimeNotifier(IHubContext<OfferHub> hubContext, ILogger<OfferRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOfferQuantityChangedAsync(Guid offerId, Guid saleBatchId, int remainingQuantity, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            offerId,
            saleBatchId,
            remainingQuantity
        };

        await _hubContext.Clients.Group(OfferHub.BatchGroup(saleBatchId))
            .SendAsync("OfferQuantityChanged", payload, cancellationToken);

        _logger.LogDebug("Đã phát sự kiện OfferQuantityChanged cho Offer {OfferId}: còn {Remaining}", offerId, remainingQuantity);
    }

    public async Task NotifySaleBatchActivatedAsync(Guid saleBatchId, Guid storeId, string storeName, int offerCount, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            saleBatchId,
            storeId,
            storeName,
            offerCount
        };

        await _hubContext.Clients.Group(OfferHub.StoreGroup(storeId))
            .SendAsync("SaleBatchActivated", payload, cancellationToken);

        _logger.LogInformation("Đã phát sự kiện SaleBatchActivated cho đợt {SaleBatchId}", saleBatchId);
    }

    public async Task NotifySaleBatchClosedAsync(Guid saleBatchId, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            saleBatchId
        };

        await _hubContext.Clients.Group(OfferHub.BatchGroup(saleBatchId))
            .SendAsync("SaleBatchClosed", payload, cancellationToken);

        _logger.LogInformation("Đã phát sự kiện SaleBatchClosed cho đợt {SaleBatchId}", saleBatchId);
    }

    public async Task NotifyOrderStatusChangedAsync(Guid customerUserId, Guid orderId, string status, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            orderId,
            status
        };

        await _hubContext.Clients.User(customerUserId.ToString())
            .SendAsync("OrderStatusChanged", payload, cancellationToken);

        _logger.LogDebug("Đã phát sự kiện OrderStatusChanged cho đơn {OrderId} (User {UserId}): {Status}", orderId, customerUserId, status);
    }
}
