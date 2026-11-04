using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Admin quản lý khiếu nại — FR-A04, 03-API-CONTRACT §8.
/// GET /admin/disputes · POST /admin/disputes/{id}/approve · POST /admin/disputes/{id}/reject[cite: 4]
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = "SystemAdmin")]
public class DisputesController : Controller
{
    private readonly IApiClient _api;

    public DisputesController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync(
        int? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = "20"
        };

        if (status.HasValue)
        {
            query["status"] = status.Value.ToString();
        }

        var path = QueryHelpers.AddQueryString("admin/disputes", query);
        var vm = new DisputeListViewModel();

        try
        {
            var data = await _api.GetAsync<PagedResponse<AdminDisputeDto>>(path, cancellationToken);
            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.PageSize = data.PageSize;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "Không thể tải danh sách khiếu nại.";
        }

        vm.StatusFilter = status;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAsync(
        Guid id,
        string? adminNote,
        int? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Sửa thành chỉ truyền 1 type argument <object>
            await _api.PostAsync<object>(
                $"admin/disputes/{id}/approve",
                new { adminNote },
                cancellationToken);

            TempData["Success"] = "Đã duyệt khiếu nại và kích hoạt hoàn tiền.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "Phê duyệt khiếu nại thất bại.";
        }

        return RedirectToAction("Index", new { status, page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectAsync(
        Guid id,
        string? adminNote,
        int? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Sửa thành chỉ truyền 1 type argument <object>
            await _api.PostAsync<object>(
                $"admin/disputes/{id}/reject",
                new { adminNote },
                cancellationToken);

            TempData["Success"] = "Đã từ chối khiếu nại.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "Từ chối khiếu nại thất bại.";
        }

        return RedirectToAction("Index", new { status, page });
    }
}
