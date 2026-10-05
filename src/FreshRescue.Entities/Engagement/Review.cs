using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Common;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;

namespace FreshRescue.Entities.Engagement;

/// <summary>
/// Đánh giá gắn vào đơn, quy về chi nhánh (OQ-28). Chỉ đơn Redeemed, mỗi đơn đúng một đánh giá
/// (BR-12) — <see cref="OrderId"/> unique.
/// </summary>
public class Review : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid CustomerUserId { get; set; }

    public Guid StoreId { get; set; }

    /// <summary>1–5.</summary>
    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public string? PhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;

    public AppUser Customer { get; set; } = null!;

    public Store Store { get; set; } = null!;
}
