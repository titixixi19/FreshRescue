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

    public List<OrderItemDto> Items { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
