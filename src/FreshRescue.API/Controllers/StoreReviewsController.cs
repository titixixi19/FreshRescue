using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/stores")]
[Produces("application/json", "application/xml")]
public sealed class StoreReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public StoreReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Danh sách đánh giá công khai của một cửa hàng, có phân trang.</summary>
    [HttpGet("{id:guid}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<ReviewDto>>> GetAllAsync(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedResponse<ReviewDto>.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _reviewService.GetStoreReviewsAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
