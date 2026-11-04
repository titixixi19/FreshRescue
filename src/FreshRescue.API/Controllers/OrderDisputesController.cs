using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Roles = AppRoles.Customer)]
public sealed class OrderDisputesController : ControllerBase
{
    private readonly IDisputeService _service;

    public OrderDisputesController(IDisputeService service) => _service = service;

    /// <summary>Khách khiếu nại đơn đã nhận trong 2 giờ, kèm ảnh (BR-17).</summary>
    [HttpPost("{id:guid}/dispute")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DisputeDto>> CreateAsync(
        Guid id, [FromBody] CreateDisputeRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
