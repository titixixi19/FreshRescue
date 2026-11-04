using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Quản lý danh mục cho Admin (FR-A03) — <c>admin/categories</c>.
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin)]
public class CategoriesController : Controller
{
    private readonly IApiClient _api;

    public CategoriesController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    [ActionName("Index")]
    public async Task<IActionResult> IndexAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        // Danh mục ít, hiện một trang dài để danh mục vừa tạo (thứ tự lớn) không bị đẩy sang trang sau.
        var vm = await LoadListAsync(page, cancellationToken, pageSize: 100);
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Create")]
    public async Task<IActionResult> CreateAsync(CategoryFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PostAsync<AdminCategoryDto>("admin/categories", ToBody(form), cancellationToken);
            TempData["Success"] = $"Đã tạo danh mục \"{form.Name}\".";
            return RedirectToAction("Index");
        }
        catch (ApiException ex)
        {
            var vm = await LoadListAsync(1, cancellationToken, pageSize: 100);
            form.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Tạo danh mục thất bại." : ex.Message;
            form.FieldErrors = ex.Errors.SelectMany(e => e.Value).ToList();
            vm.NewCategory = form;
            return View("Index", vm);
        }
    }

    [HttpGet]
    [ActionName("Edit")]
    public async Task<IActionResult> EditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // API không có GET theo id nên tìm trong danh sách (tối đa 100 danh mục).
        var vm = await LoadListAsync(1, cancellationToken, pageSize: 100);
        var c = vm.Items.FirstOrDefault(x => x.Id == id);
        if (c is null)
        {
            TempData["Error"] = vm.ErrorMessage ?? "Không tìm thấy danh mục.";
            return RedirectToAction("Index");
        }

        return View("Edit", new CategoryFormViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            IconUrl = c.IconUrl,
            DisplayOrder = c.DisplayOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Edit")]
    public async Task<IActionResult> EditPostAsync(CategoryFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PutAsync<AdminCategoryDto>($"admin/categories/{form.Id}", ToBody(form), cancellationToken);
            TempData["Success"] = $"Đã cập nhật danh mục \"{form.Name}\".";
            return RedirectToAction("Index");
        }
        catch (ApiException ex)
        {
            form.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Cập nhật danh mục thất bại." : ex.Message;
            form.FieldErrors = ex.Errors.SelectMany(e => e.Value).ToList();
            return View("Edit", form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"admin/categories/{id}", cancellationToken);
            TempData["Success"] = "Đã xoá danh mục.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message) ? "Xoá danh mục thất bại." : ex.Message;
        }
        return RedirectToAction("Index");
    }

    private static object ToBody(CategoryFormViewModel form) => new
    {
        name = form.Name,
        slug = form.Slug,
        iconUrl = string.IsNullOrWhiteSpace(form.IconUrl) ? null : form.IconUrl,
        displayOrder = form.DisplayOrder
    };

    private async Task<CategoryListViewModel> LoadListAsync(int page, CancellationToken cancellationToken, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        var vm = new CategoryListViewModel { Page = page };
        try
        {
            var path = QueryHelpers.AddQueryString("admin/categories", new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString()
            });
            var data = await _api.GetAsync<PagedResponse<AdminCategoryDto>>(path, cancellationToken);
            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Không tải được danh mục." : ex.Message;
        }
        return vm;
    }
}
