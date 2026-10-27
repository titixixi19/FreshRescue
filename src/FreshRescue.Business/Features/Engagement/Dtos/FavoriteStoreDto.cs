namespace FreshRescue.Business.Features.Engagement.Dtos;

/// <summary>
/// DTO chi nhánh yêu thích của khách hàng (FR-C12).
/// </summary>
public record FavoriteStoreDto
{
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string Ward { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string? BrandLogoUrl { get; init; }
    public decimal RatingAverage { get; init; }
    public DateTime CreatedAt { get; init; }
}
