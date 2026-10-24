namespace FreshRescue.Business.Features.Catalog.Dtos;

public record StoreNearbyDto
{
    public Guid Id { get; init; }

    public Guid BrandId { get; init; }

    public string BrandName { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string AddressLine { get; init; } = default!;

    public string District { get; init; } = default!;

    public string City { get; init; } = default!;

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    /// <summary>Server tính bằng Haversine sau khi lọc hộp bao (02-DOMAIN-MODEL.md §5.1).</summary>
    public decimal DistanceKm { get; init; }

    public TimeOnly OpenTime { get; init; }

    public TimeOnly CloseTime { get; init; }

    public decimal RatingAverage { get; init; }

    public int RatingCount { get; init; }

    public int ActiveBatchCount { get; init; }

    /// <summary>Giá thấp nhất trong các offer còn suất của đợt đang mở; <c>null</c> khi không có.</summary>
    public decimal? MinPrice { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
