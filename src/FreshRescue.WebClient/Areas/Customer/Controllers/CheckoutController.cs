using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.WebClient.Areas.Customer.Controllers;

/// <summary>
/// Quick Checkout (T4-TV4-02): đặt → thanh toán giả lập → hiện QR, chạy trong modal bằng fetch
/// (wwwroot/js/checkout.js). Luồng phải đi tới QR, nếu không nhân viên không có gì để quét (01 §9.3).
/// </summary>
/// <remarks>
/// Hai action JSON chỉ chuyển tiếp sang API. Lỗi API do <c>ApiExceptionFilter</c> đổi thành
/// JSON <c>{ errorCode, message }</c> — JS bắt theo <c>errorCode</c> (03 §1.3).
/// </remarks>
[Area("Customer")]
[Authorize(Roles = AppRoles.Customer)]
public class CheckoutController : Controller
{
    private readonly IApiClient _api;

    public CheckoutController(IApiClient api)
    {
        _api = api;
    }

    /// <summary><c>POST /orders</c> — giữ chỗ tồn kho (FR-C05).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOrderRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errorCode = ApiErrorMessages.ValidationFailed, message = "Chưa chọn món nào." });
        }

        var order = await _api.PostAsync<OrderDto>("orders", request, cancellationToken);
        return Json(order);
    }

    /// <summary>Trang thanh toán cho đơn đặt từ trước (mở từ lịch sử đơn).</summary>
    [HttpGet]
    public async Task<IActionResult> PayAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _api.GetAsync<OrderDto>($"me/orders/{id}", cancellationToken);
        if (order.Status != OrderStatus.PendingPayment)
        {
            return RedirectToAction("Details", "Orders", new { id });
        }

        return View(order);
    }

    /// <summary><c>POST /orders/{id}/payment</c> — cổng giả hai nút (OQ-17). Thành công thì có kèm vé QR.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayAsync(Guid id, [FromBody] PayOrderRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errorCode = ApiErrorMessages.ValidationFailed, message = "Thiếu kết quả thanh toán." });
        }

        var payment = await _api.PostAsync<OrderPaymentDto>($"orders/{id}/payment", request, cancellationToken);
        return Json(payment);
    }
}
