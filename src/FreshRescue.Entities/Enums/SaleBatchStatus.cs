namespace FreshRescue.Entities.Enums;

/// <summary>
/// Máy trạng thái ở 02-DOMAIN-MODEL.md §4.1. Không có chuyển trạng thái tại SaleEndTime —
/// hết giờ bán do BR-02 kiểm theo thời gian.
/// </summary>
public enum SaleBatchStatus : byte
{
    Draft = 0,
    Scheduled = 1,
    Active = 2,
    Closed = 3,
    Cancelled = 4,
}
