using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Customer.Models;

// —— DTO theo 03-API-CONTRACT.md §3 và §5 ——

/// <summary>Một dòng của <c>GET /stores/nearby</c> (FR-C02).</summary>
public record StoreNearbyDto
{
    public Guid Id { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string? District { get; init; }
    public string? City { get; init; }
    public double DistanceKm { get; init; }
    public TimeOnly OpenTime { get; init; }
    public TimeOnly CloseTime { get; init; }
    public decimal RatingAverage { get; init; }
    public int RatingCount { get; init; }
    public int ActiveBatchCount { get; init; }
    public decimal? MinPrice { get; init; }
}

/// <summary><c>GET /stores/{id}</c> — chi nhánh kèm các đợt đang mở (FR-C04).</summary>
public record StoreDetailDto
{
    public Guid Id { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string? Ward { get; init; }
    public string? District { get; init; }
    public string? City { get; init; }
    public string? Phone { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public TimeOnly OpenTime { get; init; }
    public TimeOnly CloseTime { get; init; }
    public decimal RatingAverage { get; init; }
    public int RatingCount { get; init; }
    public IReadOnlyList<StoreActiveBatchDto> ActiveBatches { get; init; } = [];
}

public record StoreActiveBatchDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public DateTime SaleStartTime { get; init; }
    public DateTime SaleEndTime { get; init; }
    public DateTime PickupStartTime { get; init; }
    public DateTime PickupEndTime { get; init; }
    public IReadOnlyList<ActiveOfferDto> Offers { get; init; } = [];
}

/// <summary>Một đánh giá của <c>GET /stores/{id}/reviews</c>.</summary>
public record StoreReviewDto
{
    public Guid Id { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public string? PhotoUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Một dòng của <c>GET /me/favorites</c> (FR-C12).</summary>
public record FavoriteStoreDto
{
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string? District { get; init; }
    public string? City { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string? BrandLogoUrl { get; init; }
    public decimal RatingAverage { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Một dòng của <c>GET /me/notifications</c> (FR-C13).</summary>
public record NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Guid? RelatedEntityId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

// —— View model ——

public class NearbyStoresViewModel
{
    public double Lat { get; init; }
    public double Lng { get; init; }
    public double RadiusKm { get; init; }
    public bool HasActiveBatch { get; init; }

    /// <summary>Chưa có vị trí thật của khách thì tìm quanh trung tâm Quận 1.</summary>
    public bool IsDefaultLocation { get; init; }
    public required PagedResponse<StoreNearbyDto> Stores { get; init; }
}

public class StoreDetailViewModel
{
    public required StoreDetailDto Store { get; init; }
    public required PagedResponse<StoreReviewDto> Reviews { get; init; }

    /// <summary><c>null</c> = chưa đăng nhập bằng tài khoản khách, không hiện nút yêu thích.</summary>
    public bool? IsFavorite { get; init; }
}

public class NotificationListViewModel
{
    public bool UnreadOnly { get; init; }
    public required PagedResponse<NotificationDto> Notifications { get; init; }
}
