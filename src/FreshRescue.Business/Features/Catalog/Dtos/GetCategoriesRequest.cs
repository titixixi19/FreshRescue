using FreshRescue.Business.Common;

namespace FreshRescue.Business.Features.Catalog.Dtos;

public record GetCategoriesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedResponse<CategoryDto>.DefaultPageSize;
}
