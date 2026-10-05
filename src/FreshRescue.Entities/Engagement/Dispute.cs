using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;

namespace FreshRescue.Entities.Engagement;

/// <summary>
/// Khiếu nại sau khi nhận hàng: chỉ đơn Redeemed, trong 2 giờ kể từ RedeemedAt, mỗi đơn một
/// khiếu nại (BR-17). Chỉ SystemAdmin được duyệt / từ chối (OQ-25).
/// </summary>
public class Dispute : BaseEntity, IAuditable
{
    public Guid OrderId { get; set; }

    public Guid CustomerUserId { get; set; }

    public DisputeReason Reason { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Ảnh bằng chứng, bắt buộc.</summary>
    public string PhotoUrl { get; set; } = string.Empty;

    public DisputeStatus Status { get; set; } = DisputeStatus.Open;

    /// <summary>
    /// Brand Manager / Staff chỉ được giải trình ở đây, không được tự quyết — tránh xung đột lợi
    /// ích (OQ-25).
    /// </summary>
    public string? MerchantNote { get; set; }

    public string? AdminNote { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAt { get; set; }

    /// <summary>Để Admin xếp hàng đợi khiếu nại theo thời gian.</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;

    public AppUser Customer { get; set; } = null!;
}
