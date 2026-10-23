using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Đơn trong đợt — GET staff/sale-batches/{id}/orders (03 §6.5).
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.BrandManager + "," + AppRoles.StoreStaff)]
public class StaffOrdersController : Controller
{
    private readonly IApiClient _api;

    public StaffOrdersController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid saleBatchId,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        var vm = new StaffOrderListViewModel
        {
            SaleBatchId = saleBatchId,
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

            var path = QueryHelpers.AddQueryString(
                $"staff/sale-batches/{saleBatchId}/orders",
                query);

            var data = await _api.GetAsync<PagedResponse<StaffOrderDto>>(path, cancellationToken);
            vm.Items = data.Items;
            vm.Page = data.Page;
            vm.PageSize = data.PageSize;
            vm.TotalCount = data.TotalCount;
            vm.TotalPages = data.TotalPages;
        }
        catch (ApiException ex)
        {
            vm.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Thao tác API thất bại."
                : ex.Message;
        }

        return View(vm);
    }
}
