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

    /// <summary>Hàng đợi khiếu nại cho Admin, lọc theo <c>status</c> (FR-A04).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DisputeDto>), StatusCodes.Status200OK)]
    public Task<PagedResponse<DisputeDto>> GetAsync(
        [FromQuery] DisputeStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => _service.GetAdminAsync(status, page, pageSize, ct);

    /// <summary>Duyệt khiếu nại → hoàn 100% cho khách (FR-A04, OQ-25, BR-21).</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status200OK)]
    public Task<DisputeDto> ApproveAsync(Guid id, CancellationToken ct)
        => _service.ApproveAsync(id, ct);

    /// <summary>Từ chối khiếu nại, bắt buộc kèm <c>adminNote</c> (FR-A04).</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status200OK)]
    public Task<DisputeDto> RejectAsync(
        Guid id, [FromBody] RejectDisputeRequest request, CancellationToken ct)
        => _service.RejectAsync(id, request, ct);
}
