using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

public interface IBrandStaffService
{
    Task<PagedResponse<BrandStaffDto>> GetAllAsync(
        GetBrandStaffRequest request,
        CancellationToken cancellationToken);

    Task<BrandStaffDto> CreateAsync(
        CreateBrandStaffRequest request,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid id,
        CancellationToken cancellationToken);
}
