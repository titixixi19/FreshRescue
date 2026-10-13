using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Entities.Catalog;

/// <summary>
/// Mẫu mặt hàng của cả chuỗi (OQ-10). KHÔNG có số lượng — số lượng nằm ở Offer, nơi xảy ra
/// tranh chấp tồn kho (01-REQUIREMENTS.md §3.1). Sửa mẫu không làm đổi các Offer đã tạo (BR-18).
/// </summary>
public class OfferTemplate : BaseEntity, ISoftDelete, IAuditable
{
    public Guid BrandId { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public OfferType OfferType { get; set; }

    /// <summary>Giá niêm yết ban ngày.</summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>Giá xả kho khuyến nghị của brand.</summary>
    public decimal SuggestedPrice { get; set; }

    /// <summary>
    /// Tầng brand của BR-07: chỉ được siết chặt hơn biên độ tầng sàn, không được nới rộng.
    /// </summary>
    public decimal BrandMinDiscountPercent { get; set; }

    public decimal BrandMaxDiscountPercent { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Brand Brand { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
