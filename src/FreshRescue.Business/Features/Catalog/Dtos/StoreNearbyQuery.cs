using FreshRescue.Business.Common;

namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>Tham số query của <c>GET /stores/nearby</c>. <c>Lat</c>, <c>Lng</c> bắt buộc.</summary>
public record StoreNearbyQuery
{
    public const decimal DefaultRadiusKm = 5;
    public const decimal MaxRadiusKm = 50;

    public decimal? Lat { get; init; }

    public decimal? Lng { get; init; }

    public decimal RadiusKm { get; init; } = DefaultRadiusKm;

    public Guid? CategoryId { get; init; }

    /// <summary><c>true</c> = chỉ chi nhánh đang có đợt bán mở.</summary>
    public bool HasActiveBatch { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedResponse<object>.DefaultPageSize;
}
