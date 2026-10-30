using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>Truy vấn danh mục công khai cho OData. Không thay đổi dữ liệu.</summary>
public interface IPublicCatalogODataService
{
    IQueryable<StoreODataDto> GetStores();
    IQueryable<OfferODataDto> GetOffers();
}
