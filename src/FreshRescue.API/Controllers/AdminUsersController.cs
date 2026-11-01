using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.SystemAdmin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service)
    {
        _service = service;
    }

    /// <summary>Admin xem danh sách tài khoản, lọc vai trò, trạng thái và tìm kiếm.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminUserDto>>> GetAllAsync(
        [FromQuery] GetAdminUsersRequest request, CancellationToken ct)
        => Ok(await _service.GetAllAsync(request, ct));

    /// <summary>Admin khóa hoặc mở khóa một tài khoản.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStatusAsync(
        Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        await _service.UpdateStatusAsync(id, request.IsActive, ct);
        return NoContent();
    }
}
