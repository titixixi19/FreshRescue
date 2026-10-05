namespace FreshRescue.Entities.Enums;

/// <summary>
/// 02-DOMAIN-MODEL.md §3.7 chỉ ghi <c>Type tinyint</c>, chưa liệt kê giá trị. Ba giá trị dưới đây
/// lấy đúng ba loại thông báo có trong yêu cầu — chờ nhóm duyệt trước Migration #1.
/// </summary>
public enum NotificationType : byte
{
    /// <summary>Chi nhánh yêu thích mở đợt bán mới (FR-C13, OQ-29).</summary>
    NewSaleBatch = 1,

    /// <summary>Nhắc 30 phút trước giờ đóng nhận hàng (FR-X05).</summary>
    PickupReminder = 2,

    /// <summary>Cửa hàng huỷ đơn đã thanh toán (BR-16).</summary>
    OrderCancelledByStore = 3,
}
