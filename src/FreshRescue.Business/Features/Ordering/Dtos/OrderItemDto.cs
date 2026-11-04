namespace FreshRescue.Business.Features.Ordering.Dtos;

public record OrderItemDto
{
    public Guid OfferId { get; init; }

    /// <summary>Tên chụp lúc đặt (BR-18) — không đổi theo template.</summary>
    public string OfferName { get; init; } = default!;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}
