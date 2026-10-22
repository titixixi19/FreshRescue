using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

public interface IBrandProfileService
{
    Task<BrandProfileDto> GetProfileAsync(
        CancellationToken cancellationToken);

    Task<BrandProfileDto> UpdateProfileAsync(
        UpdateBrandProfileRequest request,
        CancellationToken cancellationToken);
}
