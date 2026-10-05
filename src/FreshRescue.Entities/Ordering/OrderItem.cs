using FreshRescue.Entities.Common;
using FreshRescue.Entities.Sales;

namespace FreshRescue.Entities.Ordering;

/// <summary>
/// Giá và tên chụp lại lúc đặt (02-DOMAIN-MODEL.md §7): Offer có thể bị xoá mềm sau này,
/// đơn cũ vẫn phải hiển thị đủ.
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid OfferId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Chụp <c>Offer.ActualPrice</c> lúc đặt.</summary>
    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public string OfferNameSnapshot { get; set; } = string.Empty;

    public Order Order { get; set; } = null!;

    public Offer Offer { get; set; } = null!;
}
