namespace FreshRescue.WebClient.Models.Api;

// 03-API-CONTRACT.md §3.

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>Một dòng của <c>GET /offers/active</c>, cũng là kết quả <c>GET /offers/{id}</c>.</summary>
public record ActiveOfferDto
{
    public Guid Id { get; init; }
    public Guid SaleBatchId { get; init; }
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;

    /// <summary>Chỉ có khi gửi kèm <c>lat</c>/<c>lng</c>.</summary>
    public double? DistanceKm { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>Đường dẫn tương đối (OQ-09) — ghép host bằng <c>ApiOptions.ResolveUrl</c>.</summary>
    public string? ImageUrl { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public OfferType OfferType { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal ActualPrice { get; init; }
    public double DiscountPercent { get; init; }
    public int RemainingQuantity { get; init; }
    public int MaxPerCustomer { get; init; }
    public DateTime SaleStartTime { get; init; }
    public DateTime SaleEndTime { get; init; }
    public DateTime PickupStartTime { get; init; }
    public DateTime PickupEndTime { get; init; }
}
