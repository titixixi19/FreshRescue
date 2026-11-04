using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/staff/disputes")]
[Authorize(Roles = AppRoles.StoreStaff + "," + AppRoles.BrandManager)]
public sealed class StaffDisputesController : ControllerBase
{
    private readonly IDisputeService _service;

    public StaffDisputesController(IDisputeService service) => _service = service;

    /// <summary>StoreStaff/BrandManager chỉ ghi giải trình, không được duyệt.</summary>
    [HttpPatch("{id:guid}/merchant-note")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status200OK)]
    public Task<DisputeDto> SetMerchantNoteAsync(
        Guid id, [FromBody] MerchantNoteRequest request, CancellationToken ct)
        => _service.SetMerchantNoteAsync(id, request, ct);
}
