using FreshRescue.Business.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>Tham số query của <c>GET /offers/active</c>.</summary>
public record ActiveOfferListQuery
{
    public Guid? StoreId { get; init; }

    public Guid? CategoryId { get; init; }

    public OfferType? OfferType { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    /// <summary><c>true</c> = bỏ offer đã hết suất.</summary>
    public bool InStockOnly { get; init; }

    /// <summary>Vị trí khách. Có <c>Lat</c> + <c>Lng</c> thì lọc theo bán kính và trả <c>distanceKm</c>.</summary>
    public decimal? Lat { get; init; }

    public decimal? Lng { get; init; }

    /// <summary>Chỉ dùng khi có toạ độ; mặc định và giới hạn giống <c>/stores/nearby</c>.</summary>
    public decimal RadiusKm { get; init; } = StoreNearbyQuery.DefaultRadiusKm;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedResponse<object>.DefaultPageSize;
}
