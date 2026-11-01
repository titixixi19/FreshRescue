using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Quản lý dành cho Brand Manager: Mẫu mặt hàng, Thống kê, Nhân viên (03 §7).
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.BrandManager)]
public class BrandManagementController : Controller
{
    private readonly IApiClient _api;

    public BrandManagementController(IApiClient api)
    {
        _api = api;
    }

    // ——— 1. Quản lý mẫu mặt hàng (/brand/offer-templates) ———

    [HttpGet]
    public async Task<IActionResult> OfferTemplates(string? search, Guid? categoryId, int? offerType, int page = 1, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = "20"
        };
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search.Trim();
        if (categoryId.HasValue && categoryId.Value != Guid.Empty) query["categoryId"] = categoryId.Value.ToString();
        if (offerType.HasValue) query["offerType"] = offerType.Value.ToString();

        var path = QueryHelpers.AddQueryString("brand/offer-templates", query);
        var vm = new OfferTemplateListViewModel { SearchQuery = search, CategoryId = categoryId, OfferType = offerType, Page = page };

        try
        {
            var data = await _api.GetAsync<PagedResponse<OfferTemplateDto>>(path, cancellationToken);
            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.PageSize = data.PageSize;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Không thể tải danh sách mẫu mặt hàng." : ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOfferTemplate(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"brand/offer-templates/{id}", cancellationToken);
            TempData["Success"] = "Đã xoá mẫu mặt hàng.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message) ? "Xoá mẫu mặt hàng thất bại." : ex.Message;
        }
        return RedirectToAction(nameof(OfferTemplates));
    }

    // ——— 2. Thống kê Brand (/brand/stats) ———

    [HttpGet]
    public async Task<IActionResult> Stats(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>();
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");

        var path = QueryHelpers.AddQueryString("brand/stats", query);
        var vm = new BrandStatsViewModel { FromDate = from, ToDate = to };

        try
        {
            var data = await _api.GetAsync<BrandStatsDto>(path, cancellationToken);
            vm.Stats = data;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Không thể tải dữ liệu thống kê." : ex.Message;
        }

        return View(vm);
    }

    // ——— 3. Quản lý nhân viên (/brand/staff) ———

    [HttpGet]
    public async Task<IActionResult> Staff(Guid? storeId, int page = 1, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = "20"
        };
        if (storeId.HasValue && storeId.Value != Guid.Empty) query["storeId"] = storeId.Value.ToString();

        var path = QueryHelpers.AddQueryString("brand/staff", query);
        var vm = new BrandStaffListViewModel { StoreIdFilter = storeId, Page = page };

        try
        {
            var data = await _api.GetAsync<PagedResponse<BrandStaffDto>>(path, cancellationToken);
            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.PageSize = data.PageSize;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Không thể tải danh sách nhân viên." : ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStaff(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"brand/staff/{id}", cancellationToken);
            TempData["Success"] = "Đã gỡ nhân viên khỏi chi nhánh.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message) ? "Gỡ nhân viên thất bại." : ex.Message;
        }
        return RedirectToAction(nameof(Staff));
    }
}
