using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Entities.Ordering;

/// <summary>
/// Quan hệ 1—1 với <see cref="Order"/> (OQ-21). Token tự chứa dữ liệu và ký HMAC, chỉ
/// QrService giữ khoá (OQ-22) — không phải mã tra cứu CSDL (02-DOMAIN-MODEL.md §6.5).
/// </summary>
public class QrTicket : BaseEntity
{
    public Guid OrderId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Bằng <c>SaleBatch.PickupEndTime</c> lúc cấp vé. Order không có cột PickupEndTime riêng.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public QrTicketStatus Status { get; set; } = QrTicketStatus.Active;

    /// <summary>
    /// Hai cột vết đối soát thoả NFR-05 — lý do không cần bảng nhật ký đối soát riêng.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    public Guid? UsedByStaffUserId { get; set; }

    public Order Order { get; set; } = null!;
}
