using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Admin quản lý người dùng — FR-A02.
/// GET admin/users · PATCH admin/users/{id}/status (03-API-CONTRACT §8).
/// Không có endpoint Admin tạo user trong contract → không làm form tạo.
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly IApiClient _api;

    public UsersController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? role,
        bool? isActive,
        string? q,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var vm = new UserListViewModel
        {
            Role = role,
            IsActive = isActive,
            Query = q,
            Page = page
        };

        try
        {
            var query = new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = "20"
            };
            if (!string.IsNullOrWhiteSpace(role))
                query["role"] = role.Trim();
            if (isActive is not null)
                query["isActive"] = isActive.Value ? "true" : "false";
            if (!string.IsNullOrWhiteSpace(q))
                query["q"] = q.Trim();

            var path = QueryHelpers.AddQueryString("admin/users", query);
            var data = await _api.GetAsync<PagedResponse<AdminUserDto>>(path, cancellationToken);

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
    public async Task<IActionResult> SetStatus(
        Guid id,
        bool isActive,
        string? role,
        bool? filterIsActive,
        string? q,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.PatchAsync<object>(
                $"admin/users/{id}/status",
                new UpdateUserStatusRequest { IsActive = isActive },
                cancellationToken);

            TempData["Success"] = isActive ? "Đã mở khoá tài khoản." : "Đã khoá tài khoản.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction(nameof(Index), new
        {
            role,
            isActive = filterIsActive,
            q,
            page
        });
    }

    private static string FormatApiError(ApiException ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.Message))
            return ex.Message;
        return "Thao tác API thất bại.";
    }
}
