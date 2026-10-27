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

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedResponse<object>.DefaultPageSize;
}
