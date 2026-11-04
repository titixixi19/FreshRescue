using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/me/orders")]
[Produces("application/json", "application/xml")]
[Authorize(Roles = AppRoles.Customer)]
public class MeOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public MeOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Lịch sử đơn của tôi, mới nhất trước (FR-C09).</summary>
    /// <response code="200">Một trang đơn hàng</response>
    /// <response code="400">status, page hoặc pageSize không hợp lệ</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetListAsync(
        [FromQuery] OrderListQuery query,
        CancellationToken cancellationToken)
    {
        return await _orderService.GetMyOrdersAsync(query, cancellationToken);
    }

    /// <summary>Chi tiết một đơn của tôi (FR-C09).</summary>
    /// <response code="200">Đơn hàng</response>
    /// <response code="404">Không tìm thấy đơn của bạn</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _orderService.GetMyOrderAsync(id, cancellationToken);
    }
}
