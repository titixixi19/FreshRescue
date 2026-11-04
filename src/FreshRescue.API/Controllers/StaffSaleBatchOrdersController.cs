using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

/// <summary>
/// Tách khỏi controller đợt bán của Quang Huy để hai người không sửa chung một file
/// (07-WORK-BREAKDOWN.md §12.1) — cùng tiền tố route vẫn chạy bình thường.
/// </summary>
[ApiController]
[Route("api/v1/staff/sale-batches/{saleBatchId:guid}/orders")]
[Produces("application/json", "application/xml")]
[Authorize(Policy = "StaffOrBrandManager")]
public class StaffSaleBatchOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public StaffSaleBatchOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Đơn phải trả trong đợt (FR-S04). Mặc định ẩn đơn chưa thanh toán, đơn chờ trao lên đầu.</summary>
    /// <response code="200">Một trang đơn hàng</response>
    /// <response code="400">status, page hoặc pageSize không hợp lệ</response>
    /// <response code="403">Đợt bán của chi nhánh khác</response>
    /// <response code="404">Không tìm thấy đợt bán</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StaffOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<StaffOrderDto>>> GetListAsync(
        Guid saleBatchId,
        [FromQuery] OrderListQuery query,
        CancellationToken cancellationToken)
    {
        return await _orderService.GetSaleBatchOrdersAsync(saleBatchId, query, cancellationToken);
    }
}
