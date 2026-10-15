using FreshRescue.Business.Features.Sales;
using FreshRescue.Business.Features.Sales.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/staff/offers")]
[Produces("application/json")]
[Authorize(Policy = "StaffOrBrandManager")]
public class StaffOffersController : ControllerBase
{
    private readonly IOfferService _offerService;

    public StaffOffersController(IOfferService offerService)
    {
        _offerService = offerService;
    }

    /// <summary>Đổi giá bán và hạn mức mỗi khách. Giá bị khoá từ đơn Paid đầu tiên (OQ-14).</summary>
    /// <response code="200">Đã cập nhật</response>
    /// <response code="400">Giá ngoài biên độ (PRICE_OUT_OF_BAND)</response>
    /// <response code="403">Offer thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy offer</response>
    /// <response code="409">Đợt đã khoá (BATCH_LOCKED) hoặc offer vừa bị đổi (CONCURRENCY_CONFLICT)</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferDto>> UpdateAsync(
        Guid id,
        UpdateOfferRequest request,
        CancellationToken cancellationToken)
    {
        return await _offerService.UpdateAsync(id, request, cancellationToken);
    }

    /// <summary>Đặt lại tổng số suất (FR-S03). Không thấp hơn số đã giữ chỗ / đã bán; hạ về đúng số đã bán là ngừng bán (BR-24).</summary>
    /// <response code="200">Đã cập nhật số lượng</response>
    /// <response code="400">Số lượng không hợp lệ</response>
    /// <response code="403">Offer thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy offer</response>
    /// <response code="409">Hạ dưới mức đã giữ chỗ / đã bán (OFFER_HAS_ORDERS) hoặc đợt đã đóng/huỷ (BATCH_LOCKED)</response>
    [HttpPatch("{id:guid}/quantity")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferDto>> AdjustQuantityAsync(
        Guid id,
        AdjustOfferQuantityRequest request,
        CancellationToken cancellationToken)
    {
        return await _offerService.AdjustQuantityAsync(id, request, cancellationToken);
    }

    /// <summary>Xoá offer chưa có đơn (BR-24).</summary>
    /// <response code="204">Đã xoá</response>
    /// <response code="403">Offer thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy offer</response>
    /// <response code="409">Offer đã có đơn (OFFER_HAS_ORDERS)</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _offerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
