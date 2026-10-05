using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Entities.Catalog;

/// <summary>
/// Chi nhánh — địa điểm vật lý nơi khách tới lấy hàng.
/// </summary>
public class Store : BaseEntity, ISoftDelete, IAuditable
{
    public Guid BrandId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public string Ward { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    /// <summary>decimal(9,6) — đủ chính xác ~11cm cho Haversine (02-DOMAIN-MODEL.md §1).</summary>
    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string Phone { get; set; } = string.Empty;

    /// <summary>Chỉ để hiển thị, KHÔNG ràng buộc khung giờ đợt bán (OQ-05).</summary>
    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }

    public StoreStatus Status { get; set; } = StoreStatus.Active;

    /// <summary>
    /// Phi chuẩn hoá có chủ đích (02-DOMAIN-MODEL.md §7): danh sách cửa hàng là màn hình gọi
    /// nhiều nhất. Chỉ ReviewService được ghi, bằng một câu UPDATE duy nhất — không đọc rồi ghi.
    /// </summary>
    public decimal RatingAverage { get; set; }

    public int RatingCount { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Brand Brand { get; set; } = null!;

    public ICollection<StoreStaff> Staff { get; set; } = [];
}
