using FreshRescue.Business.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Admin.Dtos;

public record GetAdminBrandsRequest
{
    public BrandStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; }
        = PagedResponse<AdminBrandDto>.DefaultPageSize;
}
