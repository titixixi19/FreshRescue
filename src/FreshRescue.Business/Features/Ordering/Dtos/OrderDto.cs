using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>
/// Đơn hàng trả cho Customer (03-API-CONTRACT.md §4.1). Cố ý KHÔNG có PlatformFee và
/// StorePayoutAmount — khách không cần biết sàn ăn bao nhiêu phần trăm.
/// </summary>
public record OrderDto
{
    public Guid Id { get; init; }

    public string OrderCode { get; init; } = default!;

    public OrderStatus Status { get; init; }

    public Guid StoreId { get; init; }

    public string StoreName { get; init; } = default!;

    public string StoreAddress { get; init; } = default!;

    public Guid SaleBatchId { get; init; }

    public DateTime PickupStartTime { get; init; }

    public DateTime PickupEndTime { get; init; }

    public DateTime PaymentDeadline { get; init; }

    public decimal TotalAmount { get; init; }

    /// <summary>Chỉ có khi đã thanh toán (03-API-CONTRACT.md §4.2).</summary>
    public DateTime? PaidAt { get; init; }

    /// <summary>Bốn trường dưới chỉ có nghĩa khi đơn <c>Cancelled</c> (02-DOMAIN-MODEL.md §4.3).</summary>
    public CancelledBy? CancelledBy { get; init; }

    public string? CancelReason { get; init; }

    public DateTime? CancelledAt { get; init; }

    public RefundStatus RefundStatus { get; init; }

    public List<OrderItemDto> Items { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
