using FreshRescue.Entities.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Entities.Ordering;

/// <summary>
/// Một đơn có nhiều bản ghi: lần thanh toán hỏng, lần thành công, và bản ghi ÂM khi hoàn tiền
/// (BR-21). Số dư thực = SUM(Amount) của các bản ghi <see cref="PaymentStatus.Succeeded"/>.
/// </summary>
public class Payment : BaseEntity, IHasCreatedAt
{
    public Guid OrderId { get; set; }

    public PaymentMethod Method { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string? TransactionRef { get; set; }

    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
