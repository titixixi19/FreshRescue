using FreshRescue.API.OData;
using FreshRescue.DataAccess;
using FreshRescue.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.API.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ODataOffersController : ODataController
{
    private readonly FreshRescueDbContext _db;
    private readonly TimeProvider _clock;

    public ODataOffersController(FreshRescueDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    [HttpGet("odata/Offers")]
    [ODataAttributeRouting]
    [EnableQuery(PageSize = 20, MaxTop = 100, MaxSkip = 5000, MaxExpansionDepth = 1)]
    public IQueryable<OfferODataDto> Get()
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        return _db.Offers
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
