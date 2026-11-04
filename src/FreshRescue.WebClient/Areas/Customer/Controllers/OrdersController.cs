using System.Globalization;
using FreshRescue.WebClient.Areas.Customer.Models;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Customer.Controllers;

/// <summary>
/// Đơn của tôi: lịch sử + trạng thái + huỷ (T5-TV4-01, FR-C09, FR-C10), vé QR (FR-C07),
/// đánh giá và khiếu nại kèm ảnh (T5-TV4-02, FR-C11, FR-C15).
/// </summary>
[Area("Customer")]
[Authorize(Roles = AppRoles.Customer)]
public class OrdersController : Controller
{
    private const int PageSize = 10;

    private readonly IApiClient _api;
    private readonly TimeProvider _clock;

    public OrdersController(IApiClient api, TimeProvider clock)
    {
        _api = api;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync(OrderStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = Math.Max(page, 1).ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (status is not null)
        {
            query["status"] = status.ToString();
        }

        var orders = await _api.GetAsync<PagedResponse<OrderDto>>(
            QueryHelpers.AddQueryString("me/orders", query),
            cancellationToken);

        return View(new OrdersIndexViewModel { Status = status, Orders = orders });
    }

    [HttpGet]
    public async Task<IActionResult> DetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _api.GetAsync<OrderDto>($"me/orders/{id}", cancellationToken);
        return View(OrderDetailsViewModel.Create(order, Now));
    }

    [HttpGet]
    public async Task<IActionResult> TicketAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _api.GetAsync<OrderDto>($"me/orders/{id}", cancellationToken);
        if (order.Status != OrderStatus.Paid)
        {
            TempData["Error"] = order.Status == OrderStatus.PendingPayment
                ? "Đơn chưa thanh toán nên chưa có vé QR."
                : "Vé QR chỉ dùng được khi đơn đã thanh toán và chưa nhận hàng.";
            return RedirectToAction("Details", new { id });
        }

        var ticket = await _api.GetAsync<QrTicketDto>($"orders/{id}/qr", cancellationToken);
        return View(new OrderTicketViewModel { Order = order, Ticket = ticket });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _api.PostAsync<OrderDto>($"orders/{id}/cancel", null, cancellationToken);
            TempData["Success"] = order.RefundStatus == RefundStatus.Refunded
                ? $"Đã huỷ đơn {order.OrderCode}. Bạn được hoàn 100% số tiền đã thanh toán."
                : $"Đã huỷ đơn {order.OrderCode}.";
        }
        catch (ApiException ex) when (ex.Status is StatusCodes.Status400BadRequest or StatusCodes.Status409Conflict)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewAsync(Guid id, [Bind(Prefix = "Review")] ReviewForm form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var photoUrl = form.Photo is null
                    ? null
                    : (await _api.UploadImageAsync(form.Photo, "reviews", cancellationToken)).Url;

                await _api.PostAsync(
                    $"orders/{id}/review",
                    new CreateReviewRequest(form.Rating, string.IsNullOrWhiteSpace(form.Comment) ? null : form.Comment.Trim(), photoUrl),
                    cancellationToken);

                TempData["Success"] = "Cảm ơn bạn đã đánh giá!";
                return RedirectToAction("Details", new { id });
            }
            catch (ApiException ex) when (ex.Status is >= 400 and < 500 and not StatusCodes.Status401Unauthorized)
            {
                ModelState.AddModelError("Review", ex.Message);
            }
        }

        var order = await _api.GetAsync<OrderDto>($"me/orders/{id}", cancellationToken);
        return View("Details", OrderDetailsViewModel.Create(order, Now, review: form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisputeAsync(Guid id, [Bind(Prefix = "Dispute")] DisputeForm form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var photo = await _api.UploadImageAsync(form.Photo!, "disputes", cancellationToken);
                await _api.PostAsync(
                    $"orders/{id}/dispute",
                    new CreateDisputeRequest(form.Reason!.Value, form.Description!.Trim(), photo.Url),
                    cancellationToken);

                TempData["Success"] = "Đã gửi khiếu nại. Quản trị viên sẽ xem xét và phản hồi sớm.";
                return RedirectToAction("Details", new { id });
            }
            catch (ApiException ex) when (ex.Status is >= 400 and < 500 and not StatusCodes.Status401Unauthorized)
            {
                ModelState.AddModelError("Dispute", ex.Message);
            }
        }

        var order = await _api.GetAsync<OrderDto>($"me/orders/{id}", cancellationToken);
        return View("Details", OrderDetailsViewModel.Create(order, Now, dispute: form));
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;
}
