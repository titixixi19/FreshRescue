using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

public class BrandStaffService : IBrandStaffService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public BrandStaffService(
        IUnitOfWork unitOfWork,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<PagedResponse<BrandStaffDto>> GetAllAsync(
        GetBrandStaffRequest request,
        CancellationToken cancellationToken)
    {
        var brandId = await GetOwnedBrandIdAsync(cancellationToken);

        if (request.StoreId.HasValue)
        {
            var storeBelongsToBrand = await _unitOfWork.Stores
                .Query()
                .AsNoTracking()
                .AnyAsync(s => s.Id == request.StoreId.Value && s.BrandId == brandId, cancellationToken);

            if (!storeBelongsToBrand)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ForbiddenScope,
                    "Chi nhánh không thuộc thương hiệu của bạn.");
            }
        }

        var query = _unitOfWork.StoreStaff
            .Query()
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Store)
            .Where(s => s.Store.BrandId == brandId);

        if (request.StoreId.HasValue)
        {
            query = query.Where(s => s.StoreId == request.StoreId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<BrandStaffDto>
        {
            Items = _mapper.Map<IReadOnlyList<BrandStaffDto>>(items),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<BrandStaffDto> CreateAsync(
        CreateBrandStaffRequest request,
        CancellationToken cancellationToken)
    {
        var brandId = await GetOwnedBrandIdAsync(cancellationToken);

        var store = await _unitOfWork.Stores
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StoreId, cancellationToken);

        if (store == null || store.BrandId != brandId)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Chi nhánh không thuộc thương hiệu của bạn.");
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                "Email đã được sử dụng trên hệ thống.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Tạo tài khoản không thành công.";
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, firstError);
        }

        if (!await _roleManager.RoleExistsAsync(AppRoles.StoreStaff))
        {
            await _roleManager.CreateAsync(new AppRole { Name = AppRoles.StoreStaff });
        }

        await _userManager.AddToRoleAsync(user, AppRoles.StoreStaff);

        var staff = new StoreStaff
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StoreId = request.StoreId,
            Position = string.IsNullOrWhiteSpace(request.Position) ? null : request.Position.Trim(),
            IsActive = true,
        };

        await _unitOfWork.StoreStaff.AddAsync(staff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new BrandStaffDto
        {
            Id = staff.Id,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            StoreId = staff.StoreId,
            StoreName = store.Name,
            Position = staff.Position,
            IsActive = staff.IsActive,
        };
    }

    public async Task RemoveAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var brandId = await GetOwnedBrandIdAsync(cancellationToken);

        var staff = await _unitOfWork.StoreStaff
            .Query()
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken) ?? throw AppException.NotFound(
                ErrorCodes.NotFound,
                "Không tìm thấy nhân viên.");
        if (staff.Store.BrandId != brandId)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Nhân viên không thuộc thương hiệu của bạn.");
        }

        staff.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid> GetOwnedBrandIdAsync(CancellationToken cancellationToken)
    {
        var brandId = _currentUser.BrandId;
        var userId = _currentUser.UserId;

        if (!brandId.HasValue || !userId.HasValue)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                "Tài khoản không có phạm vi thương hiệu hợp lệ.");
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
