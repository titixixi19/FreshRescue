using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/brand/stats")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.BrandManager)]
public class BrandStatsController : ControllerBase
{
    private readonly IBrandStatsService _brandStatsService;

    public BrandStatsController(IBrandStatsService brandStatsService)
    {
        _brandStatsService = brandStatsService;
    }

    /// <summary>
    /// Lấy thống kê doanh thu và cứu thực phẩm của thương hiệu (FR-B06, BR-25, 03-API-CONTRACT.md §7).
    /// </summary>
    /// <param name="from">Ngày bắt đầu (yyyy-MM-dd), tính trọn ngày</param>
    /// <param name="to">Ngày kết thúc (yyyy-MM-dd), tính trọn ngày</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Dữ liệu thống kê thương hiệu</response>
    /// <response code="400">from > to hoặc định dạng không hợp lệ</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="403">Không có quyền Brand Manager</response>
    [HttpGet]
    [ProducesResponseType(typeof(BrandStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandStatsDto>> GetStatsAsync(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _brandStatsService.GetBrandStatsAsync(from, to, cancellationToken);
        return Ok(result);
    }
}
