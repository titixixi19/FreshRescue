using Microsoft.AspNetCore.Identity;

namespace FreshRescue.Entities.Identity;

/// <summary>
/// Một user có thể mang nhiều role cùng lúc (OQ-03) — ví dụ nhân viên cửa hàng vẫn đi mua
/// túi mù ở chỗ khác. Vai trò lưu ở bảng AspNetUserRoles của Identity.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        Id = Guid.NewGuid();
    }

    public string FullName { get; set; } = string.Empty;

    /// <summary>Đường dẫn tương đối, ví dụ <c>/uploads/avatars/x.jpg</c> (OQ-09).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Admin khoá tài khoản (FR-A02).</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
