using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

public class StoreService : IStoreService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public StoreService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<StoreDto>> GetAllAsync(
        GetBrandStoresRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Get BrandManager identity from JWT.
        var brandId = _currentUser.BrandId;
        var userId = _currentUser.UserId;

        if (!brandId.HasValue || !userId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Brand scope is missing.");
        }

        // 2. Verify this user owns the brand.
        var ownsBrand = await _unitOfWork.Brands
            .Query()
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == brandId.Value &&
                     x.OwnerUserId == userId.Value,
                cancellationToken);

        if (!ownsBrand)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "You do not own this brand.");
        }

        // 3. Query stores belonging to the brand.
        var query = _unitOfWork.Stores
            .Query()
            .AsNoTracking()
            .Where(x => x.BrandId == brandId.Value);

        // 4. Count before pagination.
        var totalCount = await query.CountAsync(
            cancellationToken);

        // 5. Sort and paginate.
        var stores = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 6. Return DTOs.
        return new PagedResponse<StoreDto>
        {
            Items = _mapper.Map<List<StoreDto>>(stores),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
    private async Task<Guid> GetOwnedBrandIdAsync(
    CancellationToken cancellationToken)
    {
        var brandId = _currentUser.BrandId;
        var userId = _currentUser.UserId;

        if (!brandId.HasValue || !userId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Brand scope is missing.");
        }

        var ownsBrand = await _unitOfWork.Brands
            .Query()
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == brandId.Value &&
                     x.OwnerUserId == userId.Value,
                cancellationToken);

        if (!ownsBrand)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "You do not own this brand.");
        }

        return brandId.Value;
    }
    public async Task<StoreDto> CreateAsync(
    CreateStoreRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Verify brand ownership.
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);

        // 2. Create a new store.
        var store = new Store
        {
            BrandId = brandId,

            Name = request.Name.Trim(),
            AddressLine = request.AddressLine.Trim(),
            Ward = request.Ward.Trim(),
            District = request.District.Trim(),
            City = request.City.Trim(),

            Latitude = request.Latitude,
            Longitude = request.Longitude,

            Phone = request.Phone.Trim(),

            OpenTime = request.OpenTime!.Value,
            CloseTime = request.CloseTime!.Value,

            Status = StoreStatus.Active,

            RatingAverage = 0,
            RatingCount = 0
        };

        // 3. Add to repository.
        await _unitOfWork.Stores.AddAsync(
            store,
            cancellationToken);

        // 4. Save changes.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // 5. Return DTO.
        return _mapper.Map<StoreDto>(store);
    }
    public async Task<StoreDto> UpdateAsync(
    Guid id,
    UpdateStoreRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Verify brand ownership.
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);

        // 2. Find the store.
        var store = await _unitOfWork.Stores
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (store == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Store not found.");
        }

        // 3. Check store scope.
        if (store.BrandId != brandId)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "You cannot update another brand's store.");
        }

        // 4. Update allowed fields.
        store.Name = request.Name.Trim();
        store.AddressLine = request.AddressLine.Trim();
        store.Ward = request.Ward.Trim();
        store.District = request.District.Trim();
        store.City = request.City.Trim();

        store.Latitude = request.Latitude;
        store.Longitude = request.Longitude;

        store.Phone = request.Phone.Trim();

        store.OpenTime = request.OpenTime!.Value;
        store.CloseTime = request.CloseTime!.Value;

        store.Status = request.Status!.Value;

        // Do not update:
        // BrandId, RatingAverage, RatingCount,
        // CreatedAt or IsDeleted.

        // 5. Save changes.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // 6. Return DTO.
        return _mapper.Map<StoreDto>(store);
    }
    public async Task DeleteAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        // 1. Verify brand ownership.
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);

        // 2. Find the store.
        var store = await _unitOfWork.Stores
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (store == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Store not found.");
        }

        // 3. Check store scope.
        if (store.BrandId != brandId)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "You cannot delete another brand's store.");
        }

        // 4. Mark for deletion.
        _unitOfWork.Stores.Remove(store);

        // 5. DbContext converts deletion into soft delete.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
}
