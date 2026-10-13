using FreshRescue.Entities.Common;

namespace FreshRescue.Entities.Identity;

/// <summary>
/// Xoay vòng: mỗi lần làm mới thì thu hồi token cũ và ghi <see cref="ReplacedByToken"/>.
/// Một token đã thu hồi mà bị dùng lại là dấu hiệu bị đánh cắp → thu hồi cả chuỗi
/// (02-DOMAIN-MODEL.md §3.1).
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Chuỗi ngẫu nhiên, KHÔNG phải JWT.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public AppUser User { get; set; } = null!;
}
