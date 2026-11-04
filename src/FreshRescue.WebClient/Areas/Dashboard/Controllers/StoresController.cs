using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Quản lý chi nhánh — FR-B02. Path: brand/stores (03 §7).
/// Không dùng GET brand/stores/{id} — endpoint không có trong contract.
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.BrandManager)]
public class StoresController : Controller
{
    private readonly IApiClient _api;

    public StoresController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var vm = new StoreListViewModel { Page = page };

        try
        {
            var path = QueryHelpers.AddQueryString("brand/stores", new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = "20"
            });

            var data = await _api.GetAsync<PagedResponse<StoreDto>>(path, cancellationToken);
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

    [HttpGet]
    public IActionResult Create()
    {
        return View(new StoreFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAsync(StoreFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new CreateStoreRequest
            {
                Name = model.Name.Trim(),
                AddressLine = model.AddressLine.Trim(),
                Ward = model.Ward,
                District = model.District,
                City = model.City.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Phone = model.Phone,
                OpenTime = ToApiTime(model.OpenTime),
                CloseTime = ToApiTime(model.CloseTime)
            };

            await _api.PostAsync<StoreDto>("brand/stores", request, cancellationToken);
            TempData["Success"] = "Đã tạo chi nhánh.";
            return RedirectToAction("Index");
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, FormatApiError(ex));
            return View(model);
        }
    }

    /// <summary>
    /// Nạp form sửa từ GET brand/stores (phân trang), không gọi GET theo id.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var store = await FindStoreByIdAsync(id, cancellationToken);
            if (store is null)
            {
                TempData["Error"] = "Không tìm thấy chi nhánh trong danh sách (có thể đã xoá hoặc không thuộc thương hiệu của bạn).";
                return RedirectToAction("Index");
            }

            return View(MapForm(store));
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAsync(Guid id, StoreFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new UpdateStoreRequest
            {
                Name = model.Name.Trim(),
                AddressLine = model.AddressLine.Trim(),
                Ward = model.Ward,
                District = model.District,
                City = model.City.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Phone = model.Phone,
                OpenTime = ToApiTime(model.OpenTime),
                CloseTime = ToApiTime(model.CloseTime),
                Status = model.Status
            };

            // Contract: PUT brand/stores/{id}
            await _api.PutAsync<StoreDto>($"brand/stores/{id}", request, cancellationToken);
            TempData["Success"] = "Đã cập nhật chi nhánh.";
            return RedirectToAction("Index");
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, FormatApiError(ex));
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Contract: DELETE brand/stores/{id}
            await _api.DeleteAsync($"brand/stores/{id}", cancellationToken);
            TempData["Success"] = "Đã xoá (mềm) chi nhánh.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Tìm store theo Id bằng GET brand/stores?page&amp;pageSize (duyệt trang).
    /// </summary>
    private async Task<StoreDto?> FindStoreByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var page = 1;
        var totalPages = 1;

        do
        {
            var path = QueryHelpers.AddQueryString("brand/stores", new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString()
            });

            var data = await _api.GetAsync<PagedResponse<StoreDto>>(path, cancellationToken);
            var match = data.Items.FirstOrDefault(s => s.Id == id);
            if (match is not null)
            {
                return match;
            }

            totalPages = data.TotalPages < 1 ? 1 : data.TotalPages;
            page++;
        }
        while (page <= totalPages);

        return null;
    }

    private static StoreFormViewModel MapForm(StoreDto s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        AddressLine = s.AddressLine,
        Ward = s.Ward,
        District = s.District,
        City = s.City,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        Phone = s.Phone,
        OpenTime = s.OpenTime,
        CloseTime = s.CloseTime,
        Status = s.Status
    };

    // Ô <input type="time"> gửi "07:00", còn API nhận TimeOnly dạng "07:00:00" — thiếu giây là 400.
    private static string? ToApiTime(string? value)
        => TimeOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var time)
            ? time.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
            : value;

    private static string FormatApiError(ApiException ex)
    {
        var message = string.IsNullOrWhiteSpace(ex.Message) ? "Thao tác API thất bại." : ex.Message;
        // Kèm lỗi theo từng trường để người dùng biết sửa ô nào, không chỉ "Dữ liệu không hợp lệ".
        return ex.Errors.Count == 0 ? message : message + ": " + string.Join(" ", ex.Errors.SelectMany(e => e.Value));
    }
}
