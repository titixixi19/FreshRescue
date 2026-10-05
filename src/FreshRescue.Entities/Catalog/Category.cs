using FreshRescue.Entities.Common;

namespace FreshRescue.Entities.Catalog;

/// <summary>Bánh ngọt, Bánh mặn, Đồ chay… Admin quản lý (FR-A03).</summary>
public class Category : BaseEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? IconUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
