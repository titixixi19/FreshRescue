using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/me/favorites")]
[Produces("application/json", "application/xml")]
[Authorize(Roles = AppRoles.Customer)]
public class MeFavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public MeFavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    /// <summary>Danh sách chi nhánh yêu thích của tôi (FR-C12).</summary>
    /// <response code="200">Trang danh sách chi nhánh yêu thích</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<FavoriteStoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<FavoriteStoreDto>>> GetFavoritesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _favoriteService.GetMyFavoritesAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Thêm chi nhánh vào danh sách yêu thích (FR-C12, idempotent).</summary>
    /// <response code="204">Thêm thành công hoặc đã có trong danh sách</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="404">Không tìm thấy chi nhánh</response>
    [HttpPost("{storeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavoriteAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        await _favoriteService.AddFavoriteAsync(storeId, cancellationToken);
        return NoContent();
    }

    /// <summary>Bỏ chi nhánh khỏi danh sách yêu thích (FR-C12, idempotent).</summary>
    /// <response code="204">Xoá thành công hoặc chi nhánh chưa có trong danh sách</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpDelete("{storeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavoriteAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        await _favoriteService.RemoveFavoriteAsync(storeId, cancellationToken);
        return NoContent();
    }
}
