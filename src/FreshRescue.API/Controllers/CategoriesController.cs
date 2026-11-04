using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Lấy danh sách danh mục có phân trang.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(PagedResponse<CategoryDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
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
