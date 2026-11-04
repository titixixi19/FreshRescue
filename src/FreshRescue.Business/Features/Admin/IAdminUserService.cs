using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin.Dtos;

namespace FreshRescue.Business.Features.Admin;

/// <summary>Quản lý trạng thái và tra cứu tài khoản của toàn hệ thống.</summary>
public interface IAdminUserService
{
    Task<PagedResponse<AdminUserDto>> GetAllAsync(GetAdminUsersRequest request, CancellationToken ct);
    Task UpdateStatusAsync(Guid userId, bool isActive, CancellationToken ct);
}
