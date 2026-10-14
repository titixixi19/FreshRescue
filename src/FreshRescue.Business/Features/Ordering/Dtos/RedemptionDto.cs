namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>Kết quả đối soát — đủ để nhân viên trao đúng món cho đúng người (03 §6.3).</summary>
public record RedemptionDto
{
    public Guid OrderId { get; init; }

    public string OrderCode { get; init; } = default!;

    public string CustomerName { get; init; } = default!;

    public DateTime RedeemedAt { get; init; }

    public IReadOnlyList<RedemptionItemDto> Items { get; init; } = [];
}

public record RedemptionItemDto
{
    /// <summary>Tên chụp lúc đặt (BR-18).</summary>
    public string OfferName { get; init; } = default!;

    public int Quantity { get; init; }
}
