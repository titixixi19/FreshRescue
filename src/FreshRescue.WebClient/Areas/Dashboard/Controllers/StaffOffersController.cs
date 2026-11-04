using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Offer trong đợt — PATCH .../quantity, DELETE staff/offers/{id} (03 §6).
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.BrandManager + "," + AppRoles.StoreStaff)]
public class StaffOffersController : Controller
{
    private readonly IApiClient _api;

    public StaffOffersController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> ManageAsync(Guid saleBatchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = await _api.GetAsync<SaleBatchDto>($"staff/sale-batches/{saleBatchId}", cancellationToken);
            var vm = new StaffOfferManageViewModel
            {
                SaleBatchId = saleBatchId,
                BatchName = batch.Name,
                BatchStatus = batch.Status,
                Offers = batch.Offers ?? Array.Empty<StaffOfferDto>()
            };
            return View(vm);
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
            return RedirectToAction("Index", "SaleBatches");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustQuantityAsync(
        Guid offerId,
        Guid saleBatchId,
        int totalQuantity,
        CancellationToken cancellationToken = default)
    {
        if (totalQuantity < 1)
        {
            TempData["Error"] = "totalQuantity phải ≥ 1.";
            return RedirectToAction("Manage", new { saleBatchId });
        }

        try
        {
            await _api.PatchAsync<object>(
                $"staff/offers/{offerId}/quantity",
                new AdjustOfferQuantityRequest { TotalQuantity = totalQuantity },
                cancellationToken);
            TempData["Success"] = "Đã cập nhật số lượng offer.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction("Manage", new { saleBatchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAsync(
        Guid offerId,
        Guid saleBatchId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _api.DeleteAsync($"staff/offers/{offerId}", cancellationToken);
            TempData["Success"] = "Đã xoá offer khỏi đợt.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = FormatApiError(ex);
        }

        return RedirectToAction("Manage", new { saleBatchId });
    }

    private static string FormatApiError(ApiException ex)
        => string.IsNullOrWhiteSpace(ex.Message) ? "Thao tác API thất bại." : ex.Message;
}

public class StaffOfferManageViewModel
{
    public Guid SaleBatchId { get; set; }
    public string? BatchName { get; set; }
    public string? BatchStatus { get; set; }
    public IReadOnlyList<StaffOfferDto> Offers { get; set; } = Array.Empty<StaffOfferDto>();
}
