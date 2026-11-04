using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Sales;
using FreshRescue.Business.Features.Sales.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/staff/sale-batches")]
[Produces("application/json", "application/xml")]
[Authorize(Policy = "StaffOrBrandManager")]
public class StaffSaleBatchesController : ControllerBase
{
    private readonly ISaleBatchService _saleBatchService;
    private readonly IOfferService _offerService;

    public StaffSaleBatchesController(ISaleBatchService saleBatchService, IOfferService offerService)
    {
        _saleBatchService = saleBatchService;
        _offerService = offerService;
    }

    /// <summary>Danh sách đợt bán trong phạm vi của mình, mới nhất trước.</summary>
    /// <response code="200">Danh sách phân trang</response>
    /// <response code="400">status, date, page hoặc pageSize không hợp lệ</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SaleBatchSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<SaleBatchSummaryDto>>> ListAsync(
        [FromQuery] SaleBatchListQuery query,
        CancellationToken cancellationToken)
    {
        return await _saleBatchService.ListAsync(query, cancellationToken);
    }

    /// <summary>Chi tiết một đợt bán kèm các offer.</summary>
    /// <response code="200">Đợt bán</response>
    /// <response code="403">Đợt thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy đợt</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SaleBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleBatchDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _saleBatchService.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>Tạo đợt bán ở trạng thái Draft kèm các offer (FR-S02, bước 1–3).</summary>
    /// <response code="201">Đã tạo đợt Draft</response>
    /// <response code="400">Sai mốc thời gian (BATCH_TIME_INVALID), giá ngoài biên độ (PRICE_OUT_OF_BAND) hoặc dữ liệu không hợp lệ</response>
    /// <response code="403">Thương hiệu chưa duyệt (BRAND_NOT_APPROVED) hoặc sai phạm vi chi nhánh</response>
    /// <response code="409">Trùng khung giờ bán với đợt khác (BATCH_OVERLAP)</response>
    [HttpPost]
    [ProducesResponseType(typeof(SaleBatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleBatchDto>> CreateAsync(
        CreateSaleBatchRequest request,
        CancellationToken cancellationToken)
    {
        var batch = await _saleBatchService.CreateAsync(request, cancellationToken);
        return Created($"/api/v1/staff/sale-batches/{batch.Id}", batch);
    }

    /// <summary>Sửa tên và bốn mốc thời gian. Bị khoá từ đơn Paid đầu tiên (OQ-14).</summary>
    /// <response code="200">Đã cập nhật</response>
    /// <response code="400">Mốc thời gian không hợp lệ</response>
    /// <response code="403">Đợt thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy đợt</response>
    /// <response code="409">Đợt đã khoá (BATCH_LOCKED) hoặc trùng khung giờ (BATCH_OVERLAP)</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SaleBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleBatchDto>> UpdateAsync(
        Guid id,
        UpdateSaleBatchRequest request,
        CancellationToken cancellationToken)
    {
        return await _saleBatchService.UpdateAsync(id, request, cancellationToken);
    }

    /// <summary>Kích hoạt đợt: Draft → Scheduled (FR-S02, bước 4).</summary>
    /// <response code="200">Đợt đã ở trạng thái Scheduled</response>
    /// <response code="400">Đợt chưa có offer hoặc khung giờ đã qua</response>
    /// <response code="403">Thương hiệu chưa duyệt hoặc sai phạm vi</response>
    /// <response code="404">Không tìm thấy đợt</response>
    /// <response code="409">Đợt không ở trạng thái Draft (BATCH_LOCKED) hoặc trùng khung giờ (BATCH_OVERLAP)</response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(SaleBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleBatchDto>> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _saleBatchService.ActivateAsync(id, cancellationToken);
    }

    /// <summary>Huỷ cả đợt bán (BR-23): huỷ và hoàn tiền mọi đơn của đợt. Chỉ Brand Manager.</summary>
    /// <response code="200">Đợt đã Cancelled</response>
    /// <response code="400">Thiếu lý do hoặc quá 500 ký tự</response>
    /// <response code="403">Không phải Brand Manager, hoặc đợt thuộc thương hiệu khác</response>
    /// <response code="404">Không tìm thấy đợt</response>
    /// <response code="409">Đợt đã đóng/huỷ hoặc đã có đơn trao hàng (BATCH_HAS_REDEEMED_ORDERS)</response>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "RequireBrandOwner")]
    [ProducesResponseType(typeof(SaleBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleBatchDto>> CancelAsync(
        Guid id,
        CancelSaleBatchRequest request,
        CancellationToken cancellationToken)
    {
        return await _saleBatchService.CancelAsync(id, request, cancellationToken);
    }

    /// <summary>Thêm một offer từ mẫu mặt hàng vào đợt.</summary>
    /// <response code="201">Đã thêm offer</response>
    /// <response code="400">Giá ngoài biên độ (PRICE_OUT_OF_BAND) hoặc mẫu đã có trong đợt</response>
    /// <response code="403">Đợt thuộc chi nhánh khác</response>
    /// <response code="404">Không tìm thấy đợt hoặc mẫu</response>
    /// <response code="409">Đợt đã đóng hoặc đã huỷ (BATCH_LOCKED)</response>
    [HttpPost("{id:guid}/offers")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferDto>> AddOfferAsync(
        Guid id,
        CreateSaleBatchOfferRequest request,
        CancellationToken cancellationToken)
    {
        var offer = await _offerService.AddToBatchAsync(id, request, cancellationToken);
        return Created($"/api/v1/staff/sale-batches/{id}", offer);
    }
}
