using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

public class BrandProfileService : IBrandProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public BrandProfileService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<BrandProfileDto> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        var brandId = _currentUser.BrandId;

        var userId = _currentUser.UserId;

        if (!brandId.HasValue || !userId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Tai khoan khong co pham vi thuong hieu hop le.");
        }

        var brand = await _unitOfWork.Brands
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == brandId.Value
                     && x.OwnerUserId == userId.Value,
                cancellationToken);

        if (brand == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay thuong hieu.");
        }

        return _mapper.Map<BrandProfileDto>(brand);
    }
    public async Task<BrandProfileDto> UpdateProfileAsync(
    UpdateBrandProfileRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Lay thong tin BrandManager tu JWT.
        var brandId = _currentUser.BrandId;
        var userId = _currentUser.UserId;

        if (!brandId.HasValue || !userId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Tai khoan khong co pham vi thuong hieu hop le.");
        }

        // 2. Tim Brand thuoc ve tai khoan hien tai.
        var brand = await _unitOfWork.Brands
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == brandId.Value &&
                     x.OwnerUserId == userId.Value,
                cancellationToken);

        if (brand == null)
        {
            throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Khong tim thay thuong hieu.");
        }

        // 3. Chuan hoa du lieu.
        var newName = request.Name.Trim();
        var newSlug = request.Slug.Trim().ToLowerInvariant();

        // 4. Kiem tra slug trung voi Brand khac.
        var slugExists = await _unitOfWork.Brands
            .Query()
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Slug == newSlug && x.Id != brand.Id,
                cancellationToken);

        if (slugExists)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Slug thuong hieu da ton tai.");
        }

        // 5. Cap nhat cac truong duoc phep.
        brand.Name = newName;
        brand.Slug = newSlug;
        brand.LogoUrl = request.LogoUrl;
        brand.Description = request.Description;

        // Khong sua Status, OwnerUserId,
        // ApprovedAt hoac ApprovedByUserId.

        // 6. Luu thay doi.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Tra DTO.
        return _mapper.Map<BrandProfileDto>(brand);
    }
}
