using FreshRescue.Entities.Common;
using FreshRescue.Entities.Identity;

namespace FreshRescue.Entities.Catalog;

/// <summary>
/// Nguồn của claim <c>store_id</c> trong JWT. <see cref="UserId"/> unique vì một nhân viên chỉ
/// thuộc một chi nhánh (OQ-07). Chuyển chi nhánh thì token cũ vẫn mang store_id cũ tới khi hết
/// hạn — lý do access token chỉ sống 15 phút.
/// </summary>
public class StoreStaff : BaseEntity, IAuditable
{
    public Guid UserId { get; set; }

    public Guid StoreId { get; set; }

    public string? Position { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public AppUser User { get; set; } = null!;

    public Store Store { get; set; } = null!;
}
