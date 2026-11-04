namespace FreshRescue.WebClient.Models.Api;

// Chép tên từ FreshRescue.Entities.Enums — WebClient không tham chiếu project đó (00-PLAN.md §2).
// API trả enum dạng chuỗi (03-API-CONTRACT.md §1), nên chỉ TÊN phải khớp, giá trị số không quan trọng.

public enum OrderStatus
{
    PendingPayment,
    Paid,
    Redeemed,
    Cancelled,
    NoShow,
}

public enum OfferType
{
    SurpriseBag,
    ClearanceItem,
}

public enum RefundStatus
{
    None,
    Refunded,
}

public enum CancelledBy
{
    System,
    Customer,
    Store,
}

public enum DisputeReason
{
    Spoiled,
    WrongCategory,
    Missing,
    Other,
}

/// <summary>Hai nút của cổng thanh toán giả (OQ-17).</summary>
public enum PaymentOutcome
{
    Success,
    Failure,
}
