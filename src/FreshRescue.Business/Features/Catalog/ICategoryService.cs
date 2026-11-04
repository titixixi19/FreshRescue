using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

public interface ICategoryService
{
    // GET - Danh sách Category
    Task<PagedResponse<CategoryDto>> GetAllAsync(
        GetCategoriesRequest request,
        CancellationToken cancellationToken);

    // POST - Tạo Category
    Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    // PUT - Cập nhật Category
    Task<CategoryDto> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    // DELETE - Xóa mềm Category
    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
