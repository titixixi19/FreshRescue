using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Produces("application/json", "application/xml")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Đặt hàng và giữ chỗ tồn kho (FR-C05).</summary>
    /// <response code="201">Tạo đơn thành công, đơn ở trạng thái PendingPayment</response>
    /// <response code="400">Dữ liệu sai định dạng, hoặc các món không cùng một đợt bán</response>
    /// <response code="404">Không tìm thấy đợt bán hoặc mặt hàng</response>
    /// <response code="409">Đợt bán đã đóng, hết suất, vượt giới hạn mua, hoặc tranh chấp tồn kho</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(request, cancellationToken);

        // Trỏ tới GET /me/orders/{id} (03 §4) — endpoint đó nằm ở controller khác.
        return Created($"/api/v1/me/orders/{order.Id}", order);
    }

    /// <summary>Thanh toán giả lập (FR-C06, OQ-17). Thành công thì trả kèm vé QR (FR-C07).</summary>
    /// <response code="200">Đã ghi nhận kết quả. Failure: đơn vẫn PendingPayment, không có qrTicket</response>
    /// <response code="400">outcome không phải Success hoặc Failure</response>
    /// <response code="404">Không tìm thấy đơn của bạn</response>
    /// <response code="409">Đơn không còn chờ thanh toán, hoặc đã quá hạn thanh toán</response>
    [HttpPost("{id:guid}/payment")]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType(typeof(OrderPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderPaymentDto>> PayAsync(
        Guid id,
        PayOrderRequest request,
        CancellationToken cancellationToken)
    {
        return await _orderService.PayAsync(id, request, cancellationToken);
    }

    /// <summary>Khách huỷ đơn (FR-C10, BR-13). Đơn đã thanh toán được hoàn 100%.</summary>
    /// <response code="200">Đã huỷ — đơn Cancelled, tồn kho được trả lại</response>
    /// <response code="404">Không tìm thấy đơn của bạn</response>
    /// <response code="409">Quá hạn huỷ, đơn đã trao hàng / đã huỷ, hoặc đơn vừa đổi trạng thái</response>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _orderService.CancelByCustomerAsync(id, cancellationToken);
    }

    /// <summary>Lấy vé QR của đơn đã thanh toán (FR-C07).</summary>
    /// <response code="200">Vé QR — vẽ token thành ảnh, không cắt hay định dạng lại chuỗi</response>
    /// <response code="404">Không tìm thấy đơn, hoặc đơn chưa thanh toán nên chưa có vé</response>
    [HttpGet("{id:guid}/qr")]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType(typeof(QrTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QrTicketDto>> GetQrTicketAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _orderService.GetQrTicketAsync(id, cancellationToken);
    }
}
