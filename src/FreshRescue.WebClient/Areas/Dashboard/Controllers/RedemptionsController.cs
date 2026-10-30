using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Quét QR / nhập mã — POST staff/redemptions (03 §6.3).
/// Camera → { token }; nhập tay → { orderCode }.
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.BrandManager + "," + AppRoles.StoreStaff)]
public class RedemptionsController : Controller
{
    private readonly IApiClient _api;

    public RedemptionsController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new RedemptionPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedeemAsync(
        RedemptionPageViewModel model,
        CancellationToken cancellationToken = default)
    {
        var code = model.ManualCode?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            model.LastSucceeded = false;
            model.LastResultMessage = "Nhập mã đơn trước khi xác nhận.";
            return View("Index", model);
        }

        var request = new RedeemOrderRequest
        {
            OrderCode = code,
            Token = null
        };

        try
        {
            var result = await _api.PostAsync<RedeemOrderResponse>(
                "staff/redemptions",
                request,
                cancellationToken);

            model.LastSucceeded = true;
            model.LastOrderCode = result.OrderCode;
            model.LastStatus = string.IsNullOrWhiteSpace(result.Status)
                ? "Redeemed"
                : result.Status;
            model.LastCustomerName = result.CustomerName;
            model.LastResultMessage = BuildSuccessMessage(result);
            model.ManualCode = string.Empty;
        }
        catch (ApiException ex)
        {
            model.LastSucceeded = false;
            model.LastResultMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Redeem thất bại (API)."
                : ex.Message;
        }

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedeemAjaxAsync(
        [FromForm] string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { success = false, message = "Mã QR trống." });
        }

        var request = new RedeemOrderRequest
        {
            Token = code.Trim(),
            OrderCode = null
        };

        try
        {
            var result = await _api.PostAsync<RedeemOrderResponse>(
                "staff/redemptions",
                request,
                cancellationToken);

            return Json(new
            {
                success = true,
                orderCode = result.OrderCode,
                status = string.IsNullOrWhiteSpace(result.Status) ? "Redeemed" : result.Status,
                customerName = result.CustomerName,
                message = BuildSuccessMessage(result)
            });
        }
        catch (ApiException ex)
        {
            return Json(new
            {
                success = false,
                message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Redeem thất bại (API)."
                    : ex.Message
            });
        }
    }

    private static string BuildSuccessMessage(RedeemOrderResponse result)
    {
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            return result.Message;
        }

        var who = string.IsNullOrWhiteSpace(result.CustomerName)
            ? result.OrderCode
            : $"{result.OrderCode} — {result.CustomerName}";
        return $"Đã xác nhận giao hàng: {who}.";
    }
}
