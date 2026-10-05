using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
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
}
