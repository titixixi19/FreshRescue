using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>Offer hiển thị công khai cho khách (03-API-CONTRACT.md §3).</summary>
public record ActiveOfferDto
{
    public Guid Id { get; init; }

    public Guid SaleBatchId { get; init; }

    public Guid StoreId { get; init; }

    public string StoreName { get; init; } = default!;

    /// <summary>Khoảng cách tới chi nhánh (km); chỉ có khi request kèm <c>lat</c> + <c>lng</c>.</summary>
    public decimal? DistanceKm { get; init; }

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public string CategoryName { get; init; } = default!;

    public OfferType OfferType { get; init; }

    public decimal OriginalPrice { get; init; }

    public decimal ActualPrice { get; init; }

    public decimal DiscountPercent { get; init; }

    public int RemainingQuantity { get; init; }

    public int MaxPerCustomer { get; init; }

    public DateTime SaleStartTime { get; init; }

    public DateTime SaleEndTime { get; init; }

    public DateTime PickupStartTime { get; init; }

    public DateTime PickupEndTime { get; init; }
}
