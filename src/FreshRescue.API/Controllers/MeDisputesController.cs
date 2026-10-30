using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/me/disputes")]
[Authorize(Roles = AppRoles.Customer)]
public sealed class MeDisputesController : ControllerBase
{
    private readonly IDisputeService _service;

    public MeDisputesController(IDisputeService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DisputeDto>), StatusCodes.Status200OK)]
    public Task<PagedResponse<DisputeDto>> GetAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => _service.GetMineAsync(page, pageSize, ct);
}
