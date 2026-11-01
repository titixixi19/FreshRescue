using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace FreshRescue.API.Controllers;

/// <summary>Truy vấn OData công khai cho Offers, chỉ đọc.</summary>
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ODataOffersController : ODataController
{
    private readonly IPublicCatalogODataService _service;

    public ODataOffersController(IPublicCatalogODataService service)
    {
        _service = service;
    }

    [HttpGet("odata/Offers")]
    [ODataAttributeRouting]
    [EnableQuery(PageSize = 20, MaxTop = 100, MaxSkip = 5000, MaxExpansionDepth = 1)]
    public IQueryable<OfferODataDto> Get()
        => _service.GetOffers();
}
