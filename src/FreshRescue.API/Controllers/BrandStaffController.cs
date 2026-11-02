using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/brand/staff")]
[Authorize(Roles = AppRoles.BrandManager)]
public class BrandStaffController : ControllerBase
{
    private readonly IBrandStaffService _brandStaffService;

    public BrandStaffController(IBrandStaffService brandStaffService)
    {
        _brandStaffService = brandStaffService;
    }

    /// <summary>
    /// Danh sách nhân viên chi nhánh của thương hiệu (03-API-CONTRACT.md §7).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BrandStaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<BrandStaffDto>>> GetAllAsync(
        [FromQuery] GetBrandStaffRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _brandStaffService.GetAllAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo tài khoản nhân viên chi nhánh mới (03-API-CONTRACT.md §7).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BrandStaffDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandStaffDto>> CreateAsync(
        [FromBody] CreateBrandStaffRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _brandStaffService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Gỡ nhân viên khỏi chi nhánh (đặt IsActive = false) (03-API-CONTRACT.md §7).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _brandStaffService.RemoveAsync(id, cancellationToken);
        return NoContent();
    }
}
