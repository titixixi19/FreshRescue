using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/admin/brands")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public class AdminBrandsController : ControllerBase
{
    private readonly IAdminBrandService _adminBrandService;

    public AdminBrandsController(
        IAdminBrandService adminBrandService)
    {
        _adminBrandService = adminBrandService;
    }

    /// <summary>
    /// Lay danh sach thuong hieu danh cho SystemAdmin.
    /// Ho tro loc theo trang thai va phan trang.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<AdminBrandDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AdminBrandDto>>> GetAllAsync(
        [FromQuery] GetAdminBrandsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminBrandService.GetAllAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Approve a brand pending registration.
    /// </summary>
    /// <param name="id">Brand ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approved brand.</returns>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(
        typeof(AdminBrandDto),
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
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminBrandDto>> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminBrandService.ApproveAsync(
            id,
            cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Reject a pending brand registration with a reason.
    /// </summary>
    /// <param name="id">Brand ID.</param>
    /// <param name="request">Rejection reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rejected brand.</returns>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(
        typeof(AdminBrandDto),
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
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminBrandDto>> RejectAsync(
        Guid id,
        [FromBody] RejectBrandRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminBrandService.RejectAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Suspend an approved brand with a reason.
    /// </summary>
    /// <param name="id">Brand ID.</param>
    /// <param name="request">Suspension reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suspended brand.</returns>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(
        typeof(AdminBrandDto),
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
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminBrandDto>> SuspendAsync(
        Guid id,
        [FromBody] SuspendBrandRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminBrandService.SuspendAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }
}
