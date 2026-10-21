using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(
        ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Thêm danh mục mới.</summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(CategoryDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryDto>> CreateAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            category);
    }

    /// <summary>
    /// Cap nhat thong tin danh muc.
    /// </summary>
    /// <response code="200">Cap nhat thanh cong.</response>
    /// <response code="400">Du lieu khong hop le hoac slug bi trung.</response>
    /// <response code="404">Khong tim thay danh muc.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(CategoryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(category);
    }
    /// <summary>
    /// Xoa mem danh muc theo ID.
    /// </summary>
    /// <response code="204">Xoa danh muc thanh cong.</response>
    /// <response code="404">Khong tim thay danh muc.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }
    /// <summary>
    /// Lay danh sach danh muc danh cho SystemAdmin.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<CategoryDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<CategoryDto>>> GetAllAsync(
        [FromQuery] GetCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
}
