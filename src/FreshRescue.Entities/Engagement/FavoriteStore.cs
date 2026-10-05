using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Common;
using FreshRescue.Entities.Identity;

namespace FreshRescue.Entities.Engagement;

/// <summary>
/// Quan hệ M—N giữa khách và chi nhánh. Khoá chính tổ hợp (CustomerUserId, StoreId) nên không
/// kế thừa BaseEntity. Thông báo đợt bán mới chỉ gửi cho người đã yêu thích (OQ-29).
/// </summary>
public class FavoriteStore : IHasCreatedAt
{
    public Guid CustomerUserId { get; set; }

    public Guid StoreId { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser Customer { get; set; } = null!;

    public Store Store { get; set; } = null!;
}
