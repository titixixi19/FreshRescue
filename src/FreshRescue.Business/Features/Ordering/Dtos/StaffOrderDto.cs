using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>
/// Một dòng trên màn "đơn phải trả trong đợt" (FR-S04). Có tên khách để nhân viên gọi tên lúc
/// trao hàng; cố ý không có số điện thoại hay email.
/// </summary>
public record StaffOrderDto
{
    public Guid Id { get; init; }

    public string OrderCode { get; init; } = default!;

    public string CustomerName { get; init; } = default!;

    public OrderStatus Status { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTime? PaidAt { get; init; }

    public DateTime? RedeemedAt { get; init; }

    public DateTime? CancelledAt { get; init; }

    public List<OrderItemDto> Items { get; init; } = [];
}
