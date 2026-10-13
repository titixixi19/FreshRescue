namespace FreshRescue.Entities.Enums;

public enum PaymentMethod : byte
{
    MockGateway = 1,

    /// <summary>Bản ghi âm khi hoàn tiền (BR-21).</summary>
    Refund = 9,
}
