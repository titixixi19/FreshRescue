namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>Chi nhánh kèm các đợt bán đang mở (03-API-CONTRACT.md §3).</summary>
public record StoreDetailDto
{
    public Guid Id { get; init; }

    public Guid BrandId { get; init; }

    public string BrandName { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string AddressLine { get; init; } = default!;

    public string Ward { get; init; } = default!;

    public string District { get; init; } = default!;

    public string City { get; init; } = default!;

    public string Phone { get; init; } = default!;

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    public TimeOnly OpenTime { get; init; }

    public TimeOnly CloseTime { get; init; }

    public decimal RatingAverage { get; init; }

    public int RatingCount { get; init; }

    public List<StoreActiveBatchDto> ActiveBatches { get; init; } = [];
}

public record StoreActiveBatchDto
{
    public Guid Id { get; init; }

    public string? Name { get; init; }

    public DateTime SaleStartTime { get; init; }

    public DateTime SaleEndTime { get; init; }

    public DateTime PickupStartTime { get; init; }

    public DateTime PickupEndTime { get; init; }

    public List<ActiveOfferDto> Offers { get; init; } = [];
}
