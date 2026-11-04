using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.BrandManager + "," + AppRoles.StoreStaff)]
[Route("api/v1/brand/offer-templates")]
public sealed class BrandOfferTemplatesController : ControllerBase
{
    private readonly IOfferTemplateService _service;

    public BrandOfferTemplatesController(IOfferTemplateService service)
    {
        _service = service;
    }

    /// <summary>Danh sách mẫu mặt hàng của thương hiệu, lọc <c>search</c>, <c>categoryId</c>, <c>offerType</c> (FR-B04). Store Staff cũng xem được.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OfferTemplateDto>>> GetAllAsync(
        [FromQuery] GetOfferTemplatesRequest request, CancellationToken ct)
        => Ok(await _service.GetAllAsync(request, ct));

    /// <summary>Chi tiết một mẫu mặt hàng; mẫu của thương hiệu khác trả 403 (BR-11).</summary>
    [HttpGet("{id:guid}", Name = "GetBrandOfferTemplateById")]
    public async Task<ActionResult<OfferTemplateDto>> GetByIdAsync(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    /// <summary>Tạo mẫu mặt hàng; biên độ giảm giá phải nằm trong khung sàn theo loại (BR-07).</summary>
    // Nhân viên cửa hàng chỉ được xem mẫu hàng, không được tạo/sửa/xóa.
    [Authorize(Roles = AppRoles.BrandManager)]
    [HttpPost]
    public async Task<ActionResult<OfferTemplateDto>> CreateAsync(
        [FromBody] UpsertOfferTemplateRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtRoute("GetBrandOfferTemplateById", new { id = result.Id }, result);
    }

    /// <summary>Sửa mẫu mặt hàng; đợt bán đã tạo giữ nguyên bản chụp cũ (BR-18).</summary>
    [Authorize(Roles = AppRoles.BrandManager)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OfferTemplateDto>> UpdateAsync(
        Guid id, [FromBody] UpsertOfferTemplateRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    /// <summary>Xoá mềm mẫu mặt hàng (BR-22).</summary>
    [Authorize(Roles = AppRoles.BrandManager)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
