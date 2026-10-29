using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace FreshRescue.API.Controllers;

/// <summary>Truy vấn OData công khai cho Stores, chỉ đọc.</summary>
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ODataStoresController : ODataController
{
    private readonly IPublicCatalogODataService _service;

    public ODataStoresController(IPublicCatalogODataService service)
    {
        _service = service;
    }

    [HttpGet("odata/Stores")]
    [ODataAttributeRouting]
    [EnableQuery(PageSize = 20, MaxTop = 100, MaxSkip = 5000, MaxExpansionDepth = 1)]
    public IQueryable<StoreODataDto> Get()
        => _service.GetStores();
}
