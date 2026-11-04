using FreshRescue.WebClient.Areas.Customer.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Customer.Controllers;

/// <summary>Thông báo trong ứng dụng (FR-C13): đợt bán mới, nhắc giờ nhận, cửa hàng huỷ đơn.</summary>
[Area("Customer")]
[Authorize]
public class NotificationsController : Controller
{
    private const int PageSize = 15;

    private readonly IApiClient _api;

    public NotificationsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexAsync(bool unreadOnly = false, int page = 1, CancellationToken cancellationToken = default)
    {
        var path = $"me/notifications?page={Math.Max(page, 1)}&pageSize={PageSize}" + (unreadOnly ? "&unreadOnly=true" : string.Empty);
        var notifications = await _api.GetAsync<PagedResponse<NotificationDto>>(path, cancellationToken);
        return View("Index", new NotificationListViewModel { UnreadOnly = unreadOnly, Notifications = notifications });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("MarkRead")]
    public async Task<IActionResult> MarkReadAsync(Guid id, bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PatchAsync<NotificationDto>($"me/notifications/{id}/read", null, cancellationToken);
        }
        catch (ApiException ex) when (ex.Status is StatusCodes.Status404NotFound)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index", new { unreadOnly });
    }
}
