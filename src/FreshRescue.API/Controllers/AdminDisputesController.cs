using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/admin/disputes")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public sealed class AdminDisputesController : ControllerBase
{
    private readonly IDisputeService _service;

    public AdminDisputesController(IDisputeService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DisputeDto>), StatusCodes.Status200OK)]
    public Task<PagedResponse<DisputeDto>> GetAsync(
        [FromQuery] DisputeStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => _service.GetAdminAsync(status, page, pageSize, ct);

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status200OK)]
    public Task<DisputeDto> ApproveAsync(Guid id, CancellationToken ct)
        => _service.ApproveAsync(id, ct);

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status200OK)]
    public Task<DisputeDto> RejectAsync(
        Guid id, [FromBody] RejectDisputeRequest request, CancellationToken ct)
        => _service.RejectAsync(id, request, ct);
}
