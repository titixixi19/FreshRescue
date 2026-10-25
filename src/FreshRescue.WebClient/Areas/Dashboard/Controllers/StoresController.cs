using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Quản lý chi nhánh — FR-B02.
/// Path: brand/stores (03-API-CONTRACT §7). Chỉ Admin + BrandManager.
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
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
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
    public async Task<IActionResult> Create(StoreFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

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
                OpenTime = model.OpenTime,
                CloseTime = model.CloseTime
            };

            await _api.PostAsync<StoreDto>("brand/stores", request, cancellationToken);
            TempData["Success"] = "Đã tạo chi nhánh.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, FormatApiError(ex));
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Contract §7 nêu PUT/DELETE theo id; GET theo id dùng nếu API hỗ trợ.
            var store = await _api.GetAsync<StoreDto>($"brand/stores/{id}", cancellationToken);
            return View(MapForm(store));
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StoreFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

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
                OpenTime = model.OpenTime,
                CloseTime = model.CloseTime,
                Status = model.Status
            };

            await _api.PutAsync<StoreDto>($"brand/stores/{id}", request, cancellationToken);
            TempData["Success"] = "Đã cập nhật chi nhánh.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, FormatApiError(ex));
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"brand/stores/{id}", cancellationToken);
            TempData["Success"] = "Đã xoá (mềm) chi nhánh.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index));
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

    private static string FormatApiError(ApiException ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.Message))
            return ex.Message;
        return "Thao tác API thất bại.";
    }
}
