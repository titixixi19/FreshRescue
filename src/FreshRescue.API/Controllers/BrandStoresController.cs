using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/brand/stores")]
[Authorize(Roles = AppRoles.BrandManager)]
public class BrandStoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public BrandStoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    /// <summary>
    /// Get stores belonging to the current BrandManager.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<StoreDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<StoreDto>>> GetAllAsync(
        [FromQuery] GetBrandStoresRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storeService.GetAllAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Create a new store for the current brand.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(StoreDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreDto>> CreateAsync(
        [FromBody] CreateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storeService.CreateAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    /// <summary>
    /// Update an existing store owned by the current brand.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(StoreDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storeService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Soft delete a store owned by the current brand.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _storeService.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}
