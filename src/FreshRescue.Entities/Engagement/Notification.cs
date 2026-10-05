using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;

namespace FreshRescue.Entities.Engagement;

/// <summary>
/// Thông báo trong ứng dụng — không có push (DeviceToken đã bỏ 28/09, 00-PLAN.md §0).
/// </summary>
public class Notification : BaseEntity, IHasCreatedAt
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>Id của đợt bán hoặc đơn hàng liên quan, để client mở đúng màn hình.</summary>
    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser User { get; set; } = null!;
}
