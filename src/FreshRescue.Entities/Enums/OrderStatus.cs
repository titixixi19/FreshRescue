namespace FreshRescue.Entities.Enums;

/// <summary>
/// Máy trạng thái ở 02-DOMAIN-MODEL.md §4.2. "Ai huỷ" và "đã hoàn tiền chưa" là hai trường
/// riêng (<see cref="CancelledBy"/>, <see cref="RefundStatus"/>) — không thêm giá trị Refunded vào đây.
/// </summary>
public enum OrderStatus : byte
{
    PendingPayment = 0,
    Paid = 1,
    Redeemed = 2,
    Cancelled = 3,
    NoShow = 4,
}
