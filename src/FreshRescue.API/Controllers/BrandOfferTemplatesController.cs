using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.BrandManager)]
[Route("api/v1/brand/offer-templates")]
public sealed class BrandOfferTemplatesController : ControllerBase
{
    private readonly IOfferTemplateService _service;

    public BrandOfferTemplatesController(IOfferTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OfferTemplateDto>>> GetAllAsync(
        [FromQuery] GetOfferTemplatesRequest request,
        CancellationToken ct)
    {
        return Ok(await _service.GetAllAsync(request, ct));
    }

    [HttpGet("{id:guid}", Name = "GetBrandOfferTemplateById")]
    public async Task<ActionResult<OfferTemplateDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return Ok(await _service.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<OfferTemplateDto>> CreateAsync(
        [FromBody] UpsertOfferTemplateRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);

        return CreatedAtRoute(
    "GetBrandOfferTemplateById",
    new { id = result.Id },
    result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OfferTemplateDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertOfferTemplateRequest request,
        CancellationToken ct)
    {
        return Ok(await _service.UpdateAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
