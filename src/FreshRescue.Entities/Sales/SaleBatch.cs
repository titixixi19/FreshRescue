using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;

namespace FreshRescue.Entities.Sales;

/// <summary>
/// Một lần xả hàng của MỘT chi nhánh. Bốn mốc thời gian đều là <see cref="DateTime"/> UTC gắn
/// với một ngày cụ thể — không phải giờ trong ngày (01-REQUIREMENTS.md §4). Khung bán và khung
/// nhận hàng chồng lấn nhau.
/// </summary>
public class SaleBatch : BaseEntity, IAuditable
{
    public Guid StoreId { get; set; }

    public string? Name { get; set; }

    public SaleBatchStatus Status { get; set; } = SaleBatchStatus.Draft;

    public DateTime SaleStartTime { get; set; }

    public DateTime SaleEndTime { get; set; }

    public DateTime PickupStartTime { get; set; }

    /// <summary>
    /// Cấm đổi sau đơn Paid đầu tiên (OQ-14): hạn dùng đã nằm bên trong token QR đã ký, sửa
    /// cột này không làm token cũ dài thêm (01-REQUIREMENTS.md §9.4).
    /// </summary>
    public DateTime PickupEndTime { get; set; }

    /// <summary>Staff hoặc Brand Manager (OQ-01).</summary>
    public Guid CreatedByUserId { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    /// <summary>Chốt lúc đóng đợt, phục vụ số liệu ESG (OQ-15).</summary>
    public int? TotalUnsoldQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;

    public ICollection<Offer> Offers { get; set; } = [];

    public ICollection<Order> Orders { get; set; } = [];
}
