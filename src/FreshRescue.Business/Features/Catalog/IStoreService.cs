using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

public interface IStoreService
{
    // GET
    Task<PagedResponse<StoreDto>> GetAllAsync(
        GetBrandStoresRequest request,
        CancellationToken cancellationToken);

    // POST
    Task<StoreDto> CreateAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken);

    // PUT
    Task<StoreDto> UpdateAsync(
        Guid id,
        UpdateStoreRequest request,
        CancellationToken cancellationToken);

    // DELETE
    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
