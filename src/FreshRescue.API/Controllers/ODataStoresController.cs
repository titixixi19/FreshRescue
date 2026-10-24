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
public sealed class ODataStoresController : ODataController
{
    private readonly FreshRescueDbContext _db;

    public ODataStoresController(FreshRescueDbContext db)
    {
        _db = db;
    }

    [HttpGet("odata/Stores")]
    [ODataAttributeRouting]
    [EnableQuery(PageSize = 20, MaxTop = 100, MaxSkip = 5000, MaxExpansionDepth = 1)]
    public IQueryable<StoreODataDto> Get()
    {
        return _db.Stores
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
    }
}
