using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Produces("application/json", "application/xml")]
public sealed class OrderReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public OrderReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Customer đánh giá đơn đã nhận; mỗi đơn chỉ một Review (BR-12).</summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> CreateAsync(
        Guid id,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.CreateAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
