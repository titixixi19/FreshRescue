
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
                "Tài khoản không có phạm vi thương hiệu hợp lệ.");
        }

        var brand = await _unitOfWork.Brands
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == brandId.Value &&
                     x.OwnerUserId == userId.Value,
                cancellationToken)
            ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy thương hiệu.");

        return _mapper.Map<BrandProfileDto>(brand);
    }

    public async Task<BrandProfileDto> UpdateProfileAsync(
        UpdateBrandProfileRequest request,
        CancellationToken cancellationToken)
    {
        var brandId = _currentUser.BrandId;
        var userId = _currentUser.UserId;

        if (!brandId.HasValue || !userId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Tài khoản không có phạm vi thương hiệu hợp lệ.");
        }

        var brand = await _unitOfWork.Brands
            .Query()
            .FirstOrDefaultAsync(
                x => x.Id == brandId.Value &&
                     x.OwnerUserId == userId.Value,
                cancellationToken)
            ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy thương hiệu.");

        var newName = request.Name.Trim();
        var newSlug = request.Slug.Trim().ToLowerInvariant();

        // Kiểm tra cả Brand đã xóa mềm để tránh trùng Slug.
        var slugExists = await _unitOfWork.Brands
            .Query()
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Slug == newSlug &&
                     x.Id != brand.Id,
                cancellationToken);

        if (slugExists)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Đường dẫn định danh (Slug) của thương hiệu đã tồn tại.");
        }

        brand.Name = newName;
        brand.Slug = newSlug;
        brand.LogoUrl = request.LogoUrl;
        brand.Description = request.Description;

        // Không thay đổi trạng thái, chủ sở hữu hoặc thông tin phê duyệt.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return _mapper.Map<BrandProfileDto>(brand);
    }
}
