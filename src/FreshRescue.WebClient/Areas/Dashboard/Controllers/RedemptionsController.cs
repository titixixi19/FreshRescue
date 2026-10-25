using FreshRescue.WebClient.Areas.Dashboard.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Quét QR / nhập mã — POST staff/redemptions (03 §6.3).
/// Không báo thành công nếu API lỗi.
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

    /// <summary>Form nhập tay hoặc POST từ JS sau khi quét.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(
        RedemptionPageViewModel model,
        CancellationToken cancellationToken = default)
    {
        var code = model.ManualCode?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            model.LastSucceeded = false;
            model.LastResultMessage = "Nhập mã đơn hoặc quét QR trước.";
            return View("Index", model);
        }

        var request = new RedeemOrderRequest
        {
            // Contract: ưu tiên orderCode; nếu payload QR là token dài, gửi cả hai field để API chọn.
            OrderCode = code,
            QrToken = code
        };

        try
        {
            var result = await _api.PostAsync<RedeemOrderResponse>(
                "staff/redemptions",
                request,
                cancellationToken);

            model.LastSucceeded = true;
            model.LastOrderCode = result.OrderCode;
            model.LastStatus = result.Status;
            model.LastResultMessage = string.IsNullOrWhiteSpace(result.Message)
                ? $"Đã xác nhận giao hàng cho {result.OrderCode} ({result.Status})."
                : result.Message;
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

    /// <summary>
    /// Endpoint AJAX (JSON) cho quét webcam — cùng path API, không giả success.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedeemAjax(
        [FromForm] string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { success = false, message = "Mã trống." });
        }

        var request = new RedeemOrderRequest
        {
            OrderCode = code.Trim(),
            QrToken = code.Trim()
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
                status = result.Status,
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? $"OK: {result.OrderCode}"
                    : result.Message
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
}
