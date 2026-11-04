using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/me/notifications")]
[Produces("application/json", "application/xml")]
[Authorize]
public class MeNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public MeNotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Danh sách thông báo của tôi (FR-C13).</summary>
    /// <response code="200">Trang danh sách thông báo</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<NotificationDto>>> GetNotificationsAsync(
        [FromQuery] NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetMyNotificationsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Đánh dấu một thông báo là đã đọc (FR-C13).</summary>
    /// <response code="204">Đánh dấu thành công</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="404">Không tìm thấy thông báo</response>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }
}
