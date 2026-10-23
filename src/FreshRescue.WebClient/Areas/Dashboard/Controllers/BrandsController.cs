using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Admin duyệt thương hiệu — FR-A01.
/// GET admin/brands · POST .../approve|reject|suspend (03-API-CONTRACT §8).
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin)]
public class BrandsController : Controller
{
    private readonly IApiClient _api;

    public BrandsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var vm = new BrandListViewModel
        {
            StatusFilter = status,
            Page = page
        };

        try
        {
            var query = new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = "20"
            };
            if (!string.IsNullOrWhiteSpace(status))
                query["status"] = status.Trim();

            var path = QueryHelpers.AddQueryString("admin/brands", query);
            var data = await _api.GetAsync<PagedResponse<AdminBrandDto>>(path, cancellationToken);

            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.PageSize = data.PageSize;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = FormatApiError(ex);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        Guid id,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PostAsync($"admin/brands/{id}/approve", null, cancellationToken);
            TempData["Success"] = "Đã duyệt thương hiệu.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index), new { status, page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        Guid id,
        string reason,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Bắt buộc nhập lý do từ chối.";
            return RedirectToAction(nameof(Index), new { status, page });
        }

        try
        {
            await _api.PostAsync(
                $"admin/brands/{id}/reject",
                new RejectBrandRequest { Reason = reason.Trim() },
                cancellationToken);
            TempData["Success"] = "Đã từ chối thương hiệu.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index), new { status, page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(
        Guid id,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PostAsync($"admin/brands/{id}/suspend", null, cancellationToken);
            TempData["Success"] = "Đã tạm khoá thương hiệu.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index), new { status, page });
    }

    private static string FormatApiError(ApiException ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.Message))
            return ex.Message;
        return "Thao tác API thất bại.";
    }
}
