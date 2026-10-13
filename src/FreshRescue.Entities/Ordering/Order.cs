using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Common;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Sales;

namespace FreshRescue.Entities.Ordering;

/// <summary>
/// Một lần mua của một khách, tại MỘT chi nhánh, trong MỘT đợt bán (BR-03) — vì mỗi đơn chỉ
/// sinh một mã QR, quét tại một quầy.
/// </summary>
public class Order : BaseEntity, IAuditable
{
    /// <summary><c>FR-</c> + 8 ký tự, bỏ O 0 I 1 để đọc được bằng miệng (OQ-19).</summary>
    public string OrderCode { get; set; } = string.Empty;

    public Guid CustomerUserId { get; set; }

    /// <summary>
    /// Phi chuẩn hoá có chủ đích (02-DOMAIN-MODEL.md §7): BR-03 đã bảo đảm đơn chỉ thuộc một
    /// chi nhánh và một đợt; màn "đơn phải trả trong đợt" là truy vấn nóng nhất.
    /// </summary>
    public Guid StoreId { get; set; }

    public Guid SaleBatchId { get; set; }

    /// <summary>Chỉ OrderService được ghi (07-WORK-BREAKDOWN.md §12.2).</summary>
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    public CancelledBy? CancelledBy { get; set; }

    /// <summary>Bắt buộc khi cửa hàng huỷ (BR-16).</summary>
    public string? CancelReason { get; set; }

    /// <summary>Trường riêng, KHÔNG phải trạng thái — đơn Redeemed vẫn có thể Refunded (§7.1).</summary>
    public RefundStatus RefundStatus { get; set; } = RefundStatus.None;

    /// <summary>
    /// Ba cột tiền tính và đóng băng lúc tạo đơn (BR-20): PlatformFee làm tròn trước,
    /// StorePayoutAmount = TotalAmount − PlatformFee.
    /// </summary>
    public decimal TotalAmount { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal StorePayoutAmount { get; set; }

    public DateTime PaymentDeadline { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    /// <summary>Chống tranh chấp giữa "khách huỷ" và "nhân viên quét".</summary>
    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public AppUser Customer { get; set; } = null!;

    public Store Store { get; set; } = null!;

    public SaleBatch SaleBatch { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = [];

    public QrTicket? QrTicket { get; set; }

    public ICollection<Payment> Payments { get; set; } = [];

    public Review? Review { get; set; }

    public Dispute? Dispute { get; set; }
}
