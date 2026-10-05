namespace FreshRescue.Entities.Enums;

/// <summary>Chỉ có nghĩa khi đơn ở trạng thái <see cref="OrderStatus.Cancelled"/>.</summary>
public enum CancelledBy : byte
{
    System = 0,
    Customer = 1,
    Store = 2,
}
