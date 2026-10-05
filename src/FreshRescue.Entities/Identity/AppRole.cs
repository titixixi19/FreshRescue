using Microsoft.AspNetCore.Identity;

namespace FreshRescue.Entities.Identity;

/// <summary>
/// Chỉ có 4 role cố định, tên lấy từ <see cref="Constants.AppRoles"/>.
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    public AppRole()
    {
        Id = Guid.NewGuid();
    }

    public AppRole(string roleName)
        : this()
    {
        Name = roleName;
    }
}
