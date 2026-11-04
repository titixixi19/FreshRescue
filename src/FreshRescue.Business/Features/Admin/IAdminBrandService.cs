using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin.Dtos;

namespace FreshRescue.Business.Features.Admin;

public interface IAdminBrandService
{
    Task<PagedResponse<AdminBrandDto>> GetAllAsync(
        GetAdminBrandsRequest request,
        CancellationToken cancellationToken);
    Task<AdminBrandDto> ApproveAsync(
        Guid brandId,
        CancellationToken cancellationToken);
    Task<AdminBrandDto> RejectAsync(
    Guid brandId,
    RejectBrandRequest request,
    CancellationToken cancellationToken);
    Task<AdminBrandDto> SuspendAsync(
    Guid brandId,
    SuspendBrandRequest request,
    CancellationToken cancellationToken);
}
