using FreshRescue.WebClient.Areas.Customer.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Customer.Controllers;

/// <summary>Cửa hàng yêu thích (FR-C12) — chi nhánh yêu thích mở đợt mới thì khách nhận thông báo (FR-C13).</summary>
[Area("Customer")]
[Authorize(Roles = AppRoles.Customer)]
public class FavoritesController : Controller
{
    private const int PageSize = 12;

    private readonly IApiClient _api;

    public FavoritesController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        var favorites = await _api.GetAsync<PagedResponse<FavoriteStoreDto>>(
            $"me/favorites?page={Math.Max(page, 1)}&pageSize={PageSize}", cancellationToken);
        return View("Index", favorites);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Add")]
    public async Task<IActionResult> AddAsync(Guid storeId, string? returnUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PostAsync($"me/favorites/{storeId}", null, cancellationToken);
            TempData["Success"] = "Đã thêm vào cửa hàng yêu thích. Bạn sẽ được báo khi cửa hàng mở đợt bán mới.";
        }
        catch (ApiException ex) when (ex.Status is StatusCodes.Status400BadRequest or StatusCodes.Status404NotFound or StatusCodes.Status409Conflict)
        {
            TempData["Error"] = ex.Message;
        }

        return BackTo(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Remove")]
    public async Task<IActionResult> RemoveAsync(Guid storeId, string? returnUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"me/favorites/{storeId}", cancellationToken);
            TempData["Success"] = "Đã bỏ khỏi cửa hàng yêu thích.";
        }
        catch (ApiException ex) when (ex.Status is StatusCodes.Status400BadRequest or StatusCodes.Status404NotFound)
        {
            TempData["Error"] = ex.Message;
        }

        return BackTo(returnUrl);
    }

    private IActionResult BackTo(string? returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index");
    }
}
