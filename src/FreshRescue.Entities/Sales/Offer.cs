using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Entities.Sales;

/// <summary>
/// Thứ khách thật sự mua: một <see cref="OfferTemplate"/> đưa vào một <see cref="SaleBatch"/>
/// kèm số lượng thực tế và giá chốt. Bảng có tranh chấp ghi đồng thời (02-DOMAIN-MODEL.md §6).
/// </summary>
/// <remarks>
/// Tên, mô tả, ảnh, danh mục, loại và giá gốc được CHỤP LẠI từ template lúc tạo (BR-18) —
/// phi chuẩn hoá có chủ đích, đừng thay bằng khoá ngoại. Cấm xoá Offer đã có OrderItem (BR-24);
/// muốn ngừng bán thì đặt <see cref="RemainingQuantity"/> = 0.
/// </remarks>
public class Offer : BaseEntity, ISoftDelete, IAuditable
{
    public const int DefaultMaxPerCustomer = 3;

    public Guid SaleBatchId { get; set; }

    /// <summary>Chỉ để truy vết nguồn gốc — dữ liệu hiển thị lấy từ các cột chụp lại bên dưới.</summary>
    public Guid OfferTemplateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public Guid CategoryId { get; set; }

    public OfferType OfferType { get; set; }

    public decimal OriginalPrice { get; set; }

    /// <summary>Staff nhập, phải nằm trong biên độ hai tầng (BR-07).</summary>
    public decimal ActualPrice { get; set; }

    public int TotalQuantity { get; set; }

    /// <summary>
    /// Không bao giờ âm (BR-01). Chỉ OrderService được ghi (07-WORK-BREAKDOWN.md §12.2).
    /// </summary>
    public int RemainingQuantity { get; set; }

    /// <summary>Cộng dồn mọi đơn đang sống của khách trong đợt (BR-06).</summary>
    public int MaxPerCustomer { get; set; } = DefaultMaxPerCustomer;

    public int? UnsoldQuantity { get; set; }

    /// <summary>
    /// Điểm neo của cơ chế chống over-selling — cấu hình <c>IsRowVersion()</c> ở DataAccess.
    /// Đừng thay bằng cột số tự tăng tay (02-DOMAIN-MODEL.md §8).
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public SaleBatch SaleBatch { get; set; } = null!;

    public OfferTemplate OfferTemplate { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
