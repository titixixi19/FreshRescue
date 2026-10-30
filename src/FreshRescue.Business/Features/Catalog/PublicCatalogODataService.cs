using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>
/// Truy vấn chỉ đọc cho OData. Service lấy IQueryable qua repository trong IUnitOfWork.
/// Việc áp dụng $filter/$orderby/$select/$expand được thực hiện trên IQueryable trước khi chạy SQL.
/// </summary>
public sealed class PublicCatalogODataService : IPublicCatalogODataService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public PublicCatalogODataService(IUnitOfWork unitOfWork, TimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public IQueryable<StoreODataDto> GetStores()
        => _unitOfWork.Stores.Query()
            .AsNoTracking()
            .Where(s => s.Status == StoreStatus.Active
                && s.Brand.Status == BrandStatus.Approved)
            .Select(s => new StoreODataDto
            {
                Id = s.Id,
                BrandId = s.BrandId,
                BrandName = s.Brand.Name,
                Name = s.Name,
                AddressLine = s.AddressLine,
                Ward = s.Ward,
                District = s.District,
                City = s.City,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                RatingAverage = s.RatingAverage,
                RatingCount = s.RatingCount
            });

    public IQueryable<OfferODataDto> GetOffers()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        return _unitOfWork.Offers.Query()
            .AsNoTracking()
            .Where(o => o.SaleBatch.Status == SaleBatchStatus.Active
                && o.SaleBatch.SaleStartTime <= now
                && now < o.SaleBatch.SaleEndTime
                && o.RemainingQuantity > 0
                && o.SaleBatch.Store.Status == StoreStatus.Active
                && o.SaleBatch.Store.Brand.Status == BrandStatus.Approved)
            .Select(o => new OfferODataDto
            {
                Id = o.Id,
                SaleBatchId = o.SaleBatchId,
                StoreId = o.SaleBatch.StoreId,
                CategoryId = o.CategoryId,
                CategoryName = o.Category.Name,
                Name = o.Name,
                Description = o.Description,
                ImageUrl = o.ImageUrl,
                OfferType = o.OfferType == OfferType.SurpriseBag
                    ? "SurpriseBag" : "ClearanceItem",
                OriginalPrice = o.OriginalPrice,
                ActualPrice = o.ActualPrice,
                RemainingQuantity = o.RemainingQuantity,
                MaxPerCustomer = o.MaxPerCustomer,
                SaleStartTime = o.SaleBatch.SaleStartTime,
                SaleEndTime = o.SaleBatch.SaleEndTime,
                Store = new StoreODataDto
                {
                    Id = o.SaleBatch.Store.Id,
                    BrandId = o.SaleBatch.Store.BrandId,
                    BrandName = o.SaleBatch.Store.Brand.Name,
                    Name = o.SaleBatch.Store.Name,
                    AddressLine = o.SaleBatch.Store.AddressLine,
                    Ward = o.SaleBatch.Store.Ward,
                    District = o.SaleBatch.Store.District,
                    City = o.SaleBatch.Store.City,
                    Latitude = o.SaleBatch.Store.Latitude,
                    Longitude = o.SaleBatch.Store.Longitude,
                    RatingAverage = o.SaleBatch.Store.RatingAverage,
                    RatingCount = o.SaleBatch.Store.RatingCount
                }
            });
    }
}
