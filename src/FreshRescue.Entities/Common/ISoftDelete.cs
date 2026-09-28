namespace FreshRescue.Entities.Common;

/// <summary>
/// Entity xoá mềm (OQ-06). Áp cho Brand, Store, Category, OfferTemplate, Offer.
/// Truy vấn lịch sử đơn hàng phải gọi <c>IgnoreQueryFilters()</c> (BR-22).
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }
}
