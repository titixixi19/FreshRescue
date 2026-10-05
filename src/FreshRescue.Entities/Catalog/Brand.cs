using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;

namespace FreshRescue.Entities.Catalog;

/// <summary>
/// Thương hiệu tự đăng ký rồi chờ Admin duyệt (OQ-01). Người đăng ký là Brand Manager và
/// là chủ duy nhất — <see cref="OwnerUserId"/> unique vì Brand 1—1 Brand Manager.
/// </summary>
public class Brand : BaseEntity, ISoftDelete, IAuditable
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Dùng cho URL thân thiện, unique.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? Description { get; set; }

    public Guid OwnerUserId { get; set; }

    public BrandStatus Status { get; set; } = BrandStatus.PendingApproval;

    public DateTime? ApprovedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public AppUser Owner { get; set; } = null!;

    public ICollection<Store> Stores { get; set; } = [];

    public ICollection<OfferTemplate> OfferTemplates { get; set; } = [];
}
