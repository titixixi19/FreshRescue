using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/brand/profile")]
[Authorize(Roles = AppRoles.BrandManager)]
public class BrandProfileController : ControllerBase
{
    private readonly IBrandProfileService _brandProfileService;

    public BrandProfileController(
        IBrandProfileService brandProfileService)
    {
        _brandProfileService = brandProfileService;
    }

    /// <summary>
    /// Lay ho so thuong hieu cua BrandManager dang dang nhap.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(BrandProfileDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandProfileDto>> GetAsync(
        CancellationToken cancellationToken)
    {
        var result = await _brandProfileService
            .GetProfileAsync(cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Cap nhat ho so thuong hieu cua BrandManager.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(
        typeof(BrandProfileDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandProfileDto>> UpdateAsync(
        [FromBody] UpdateBrandProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _brandProfileService.UpdateProfileAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
}
