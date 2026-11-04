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
    [ActionName("OfferTemplates")]
    public async Task<IActionResult> OfferTemplatesAsync(
        string? search,
        Guid? categoryId,
        int? offerType,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = "20"
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            query["search"] = search.Trim();
        }
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query["categoryId"] = categoryId.Value.ToString();
        }
        if (offerType.HasValue)
        {
            query["offerType"] = offerType.Value.ToString();
        }

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

        return View("OfferTemplates", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("DeleteOfferTemplate")]
    public async Task<IActionResult> DeleteOfferTemplateAsync(Guid id, CancellationToken cancellationToken = default)
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
        return RedirectToAction("OfferTemplates");
    }

    // ——— 2. Thống kê Brand (/brand/stats) ———

    [HttpGet]
    [ActionName("Stats")]
    public async Task<IActionResult> StatsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>();
        if (from.HasValue)
        {
            query["from"] = from.Value.ToString("yyyy-MM-dd");
        }
        if (to.HasValue)
        {
            query["to"] = to.Value.ToString("yyyy-MM-dd");
        }

        var path = QueryHelpers.AddQueryString("brand/stats", query);
        var vm = new BrandStatsViewModel { FromDate = from, ToDate = to };

        try
        {
            vm.Stats = await _api.GetAsync<BrandStatsDto>(path, cancellationToken);
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Không thể tải dữ liệu thống kê." : ex.Message;
        }

        return View("Stats", vm);
    }

    // ——— 3. Quản lý nhân viên (/brand/staff) ———

    [HttpGet]
    [ActionName("Staff")]
    public async Task<IActionResult> StaffAsync(Guid? storeId, int page = 1, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = "20"
        };
        if (storeId.HasValue && storeId.Value != Guid.Empty)
        {
            query["storeId"] = storeId.Value.ToString();
        }

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

        vm.Stores = await LoadStoresAsync(cancellationToken);
        return View("Staff", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("CreateStaff")]
    public async Task<IActionResult> CreateStaffAsync(CreateBrandStaffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _api.PostAsync<BrandStaffDto>("brand/staff", request, cancellationToken);
            TempData["Success"] = $"Đã tạo nhân viên {created.FullName} cho {created.StoreName}.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex, "Tạo nhân viên thất bại.");
        }
        return RedirectToAction("Staff");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("DeleteStaff")]
    public async Task<IActionResult> DeleteStaffAsync(Guid id, CancellationToken cancellationToken = default)
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
        return RedirectToAction("Staff");
    }

    // Form tạo nhân viên cần chọn chi nhánh; lỗi tải chi nhánh không được chặn cả trang danh sách.
    private async Task<IReadOnlyList<StoreDto>> LoadStoresAsync(CancellationToken cancellationToken)
    {
        try
        {
            var path = QueryHelpers.AddQueryString("brand/stores", new Dictionary<string, string?>
            {
                ["page"] = "1",
                ["pageSize"] = "100"
            });
            var data = await _api.GetAsync<PagedResponse<StoreDto>>(path, cancellationToken);
            return data.Items;
        }
        catch (ApiException)
        {
            return Array.Empty<StoreDto>();
        }
    }

    private static string FormatApiError(ApiException ex, string fallback)
    {
        var message = string.IsNullOrWhiteSpace(ex.Message) ? fallback : ex.Message;
        if (ex.Errors.Count == 0)
        {
            return message;
        }
        return message + " " + string.Join(" ", ex.Errors.SelectMany(e => e.Value));
    }
}
