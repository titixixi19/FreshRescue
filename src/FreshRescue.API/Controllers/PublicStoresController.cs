using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/stores")]
[Produces("application/json")]
[AllowAnonymous]
public class PublicStoresController : ControllerBase
{
    private readonly IStorefrontService _storefrontService;

    public PublicStoresController(IStorefrontService storefrontService)
    {
        _storefrontService = storefrontService;
    }

    /// <summary>Chi nhánh gần một toạ độ, gần nhất trước (FR-C02). Lọc hộp bao rồi mới tính Haversine.</summary>
    /// <response code="200">Danh sách phân trang kèm distanceKm</response>
    /// <response code="400">Thiếu lat/lng hoặc tham số không hợp lệ (radiusKm tối đa 50)</response>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PagedResponse<StoreNearbyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<StoreNearbyDto>>> NearbyAsync(
        [FromQuery] StoreNearbyQuery query,
        CancellationToken cancellationToken)
    {
        return await _storefrontService.FindNearbyStoresAsync(query, cancellationToken);
    }

    /// <summary>Chi tiết chi nhánh kèm các đợt bán đang mở.</summary>
    /// <response code="200">Chi tiết chi nhánh</response>
    /// <response code="404">Không tìm thấy chi nhánh đang hoạt động</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _storefrontService.GetStoreAsync(id, cancellationToken);
    }
}
