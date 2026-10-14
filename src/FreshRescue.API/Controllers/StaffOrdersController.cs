using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/staff/orders")]
[Produces("application/json")]
[Authorize(Policy = "StaffOrBrandManager")]
public class StaffOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public StaffOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Cửa hàng huỷ đơn đã thanh toán do sự cố (FR-S07, BR-16): hoàn 100% và báo khách.</summary>
    /// <response code="200">Đã huỷ — đơn Cancelled, RefundStatus Refunded, vé QR hết hiệu lực</response>
    /// <response code="400">Thiếu lý do hoặc lý do quá 500 ký tự</response>
    /// <response code="403">Đơn thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy đơn</response>
    /// <response code="409">Đơn chưa thanh toán, đã trao hàng, đã huỷ, hoặc vừa đổi trạng thái</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CancelAsync(
        Guid id,
        CancelOrderByStoreRequest request,
        CancellationToken cancellationToken)
    {
        return await _orderService.CancelByStoreAsync(id, request, cancellationToken);
    }
}
