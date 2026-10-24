using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/offers")]
[Produces("application/json")]
[AllowAnonymous]
public class PublicOffersController : ControllerBase
{
    private readonly IStorefrontService _storefrontService;

    public PublicOffersController(IStorefrontService storefrontService)
    {
        _storefrontService = storefrontService;
    }

    /// <summary>Offer đang bán: đợt Active, còn trong khung giờ bán. Có thể lọc (FR-C03).</summary>
    /// <response code="200">Danh sách phân trang, hạn bán gần nhất trước</response>
    /// <response code="400">Tham số lọc hoặc phân trang không hợp lệ</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PagedResponse<ActiveOfferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ActiveOfferDto>>> ListActiveAsync(
        [FromQuery] ActiveOfferListQuery query,
        CancellationToken cancellationToken)
    {
        return await _storefrontService.ListActiveOffersAsync(query, cancellationToken);
    }

    /// <summary>Chi tiết một offer thuộc đợt đang Active.</summary>
    /// <response code="200">Chi tiết offer</response>
    /// <response code="404">Không tìm thấy offer, hoặc đợt chưa/không còn mở bán</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActiveOfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActiveOfferDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _storefrontService.GetOfferAsync(id, cancellationToken);
    }
}
