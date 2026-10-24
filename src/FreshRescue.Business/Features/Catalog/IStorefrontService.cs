using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>Các endpoint công khai để khách khám phá cửa hàng và offer (03-API-CONTRACT.md §3). Chỉ đọc.</summary>
public interface IStorefrontService
{
    /// <summary>Offer thuộc đợt Active và đang trong khung giờ bán.</summary>
    Task<PagedResponse<ActiveOfferDto>> ListActiveOffersAsync(ActiveOfferListQuery query, CancellationToken cancellationToken);

    /// <summary>Chi tiết offer thuộc đợt Active; 404 nếu không có.</summary>
    Task<ActiveOfferDto> GetOfferAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>Chi nhánh đang hoạt động kèm các đợt bán đang mở; 404 nếu không có.</summary>
    Task<StoreDetailDto> GetStoreAsync(Guid storeId, CancellationToken cancellationToken);

    /// <summary>Chi nhánh trong bán kính, gần nhất trước.</summary>
    Task<PagedResponse<StoreNearbyDto>> FindNearbyStoresAsync(StoreNearbyQuery query, CancellationToken cancellationToken);
}
