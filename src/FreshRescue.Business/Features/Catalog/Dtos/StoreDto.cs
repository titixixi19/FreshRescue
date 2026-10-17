using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

public record StoreDto
{
    public Guid Id { get; init; }
    public Guid BrandId { get; init; }

    public string Name { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string Ward { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;

    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }

    public string Phone { get; init; } = string.Empty;

    public TimeOnly OpenTime { get; init; }
    public TimeOnly CloseTime { get; init; }

    public StoreStatus Status { get; init; }

    public decimal RatingAverage { get; init; }
    public int RatingCount { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
