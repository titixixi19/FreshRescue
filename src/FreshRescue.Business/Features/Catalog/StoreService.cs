
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
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);

        var query = _unitOfWork.Stores
            .Query()
            .AsNoTracking()
            .Where(x => x.BrandId == brandId);

        var totalCount = await query.CountAsync(
            cancellationToken);

        var stores = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<StoreDto>
        {
            Items = _mapper.Map<List<StoreDto>>(stores),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StoreDto> CreateAsync(
        CreateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);
        await BrandApproval.EnsureApprovedAsync(_unitOfWork, brandId, cancellationToken);

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

        await _unitOfWork.Stores.AddAsync(
            store,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<StoreDto>(store);
    }

    public async Task<StoreDto> UpdateAsync(
        Guid id,
        UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);
        await BrandApproval.EnsureApprovedAsync(_unitOfWork, brandId, cancellationToken);

        var store = await _unitOfWork.Stores
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy chi nhánh.");

        if (store.BrandId != brandId)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Bạn không có quyền cập nhật chi nhánh thuộc thương hiệu khác.");
        }

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

        // Không cho phép thay đổi BrandId và thông tin đánh giá qua Store CRUD.

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<StoreDto>(store);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var brandId = await GetOwnedBrandIdAsync(
            cancellationToken);
        await BrandApproval.EnsureApprovedAsync(_unitOfWork, brandId, cancellationToken);

        var store = await _unitOfWork.Stores
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken)
            ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy chi nhánh.");

        if (store.BrandId != brandId)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Bạn không có quyền xóa chi nhánh thuộc thương hiệu khác.");
        }

        // DbContext chuyển thao tác Remove thành Soft Delete.

        _unitOfWork.Stores.Remove(store);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
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
                "Không xác định được thương hiệu của tài khoản.");
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
                "Bạn không có quyền quản lý thương hiệu này.");
        }

        return brandId.Value;
    }
}
