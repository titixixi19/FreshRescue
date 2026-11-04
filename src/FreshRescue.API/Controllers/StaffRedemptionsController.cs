using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/staff/redemptions")]
[Produces("application/json", "application/xml")]
[Authorize(Policy = "StaffOrBrandManager")]
public class StaffRedemptionsController : ControllerBase
{
    private readonly IOrderService _orderService;

    public StaffRedemptionsController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Đối soát: quét QR hoặc nhập mã đơn để trao hàng (FR-S05, FR-S06).</summary>
    /// <response code="200">Đã trao hàng — đơn chuyển Redeemed</response>
    /// <response code="400">Thiếu/thừa token và orderCode, QR giả hoặc hết hạn, hoặc ngoài giờ nhận hàng</response>
    /// <response code="403">Đơn thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy đơn, hoặc đơn chưa thanh toán / đã huỷ</response>
    /// <response code="409">Đơn đã được trao hàng rồi</response>
    [HttpPost]
    [ProducesResponseType(typeof(RedemptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RedemptionDto>> RedeemAsync(
        RedeemOrderRequest request,
        CancellationToken cancellationToken)
    {
        return await _orderService.RedeemAsync(request, cancellationToken);
    }
}
