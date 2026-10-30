using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Admin;

/// <summary>Tra cứu tài khoản qua IUnitOfWork; ghi trạng thái qua Identity UserManager.</summary>
public sealed class AdminUserService : IAdminUserService
{
    private static readonly string[] _validRoles =
    [
        AppRoles.SystemAdmin, AppRoles.BrandManager, AppRoles.StoreStaff, AppRoles.Customer
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public AdminUserService(
        IUnitOfWork unitOfWork,
        UserManager<AppUser> userManager,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<AdminUserDto>> GetAllAsync(
        GetAdminUsersRequest request, CancellationToken ct)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Phân trang không hợp lệ.");
        }

        var role = request.Role?.Trim();
        if (!string.IsNullOrEmpty(role) && !_validRoles.Contains(role, StringComparer.Ordinal))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Vai trò không hợp lệ.");
        }

        var search = request.Q?.Trim();
        if (search?.Length > 200)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Từ khóa tối đa 200 ký tự.");
        }

        var query = _unitOfWork.Users.Query().AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.FullName.Contains(search)
                || (x.Email != null && x.Email.Contains(search)));
        }

        if (!string.IsNullOrEmpty(role))
        {
            // Identity quản lý các vai trò, không truy vấn trực tiếp DbContext của Identity.
            // Tập ID được áp dụng trên IQueryable trước phân trang.
            var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            var ids = roleUsers.Select(x => x.Id).ToArray();
            query = query.Where(x => ids.Contains(x.Id));
        }

        var count = await query.CountAsync(ct);
        var offset = ((long)request.Page - 1) * request.PageSize;
        var users = offset > int.MaxValue
            ? new List<AppUser>()
            : await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((int)offset).Take(request.PageSize).ToListAsync(ct);

        var result = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToArray()
            });
        }

        return new PagedResponse<AdminUserDto>
        {
            Items = result,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = count
        };
    }

    public async Task UpdateStatusAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy tài khoản.");

        // Không cho Admin tự khóa chính mình khi đang đăng nhập.
        if (userId == _currentUser.UserId && !isActive)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed,
                "Không thể tự khóa tài khoản quản trị đang đăng nhập.");
        }

        if (user.IsActive == isActive)
        {
            return;
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed,
                string.Join(" ", result.Errors.Select(x => x.Description)));
        }
    }
}
