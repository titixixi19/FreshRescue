namespace FreshRescue.WebClient.Models.Api;

// 03-API-CONTRACT.md §4, §5, §12.

public record CreateOrderItemRequest(Guid OfferId, int Quantity);

public record CreateOrderRequest(Guid SaleBatchId, List<CreateOrderItemRequest> Items);

public record PayOrderRequest(PaymentOutcome Outcome);

public record OrderDto
{
    public Guid Id { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public string StoreAddress { get; init; } = string.Empty;
    public Guid SaleBatchId { get; init; }
    public DateTime PickupStartTime { get; init; }
    public DateTime PickupEndTime { get; init; }
    public DateTime PaymentDeadline { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime? PaidAt { get; init; }
    public CancelledBy? CancelledBy { get; init; }
    public string? CancelReason { get; init; }
    public DateTime? CancelledAt { get; init; }
    public RefundStatus RefundStatus { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public record OrderItemDto
{
    public Guid OfferId { get; init; }
    public string OfferName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public record OrderPaymentDto
{
    public OrderDto Order { get; init; } = new();

    /// <summary>Chỉ có khi thanh toán thành công.</summary>
    public QrTokenDto? QrTicket { get; init; }
}

public record QrTokenDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record QrTicketDto
{
    public string OrderCode { get; init; } = string.Empty;

    /// <summary>Vẽ nguyên chuỗi thành QR — không cắt, không định dạng lại (03 §4.3).</summary>
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime PickupStartTime { get; init; }
    public string StoreName { get; init; } = string.Empty;
}

public record CreateReviewRequest(int Rating, string? Comment, string? PhotoUrl);

public record CreateDisputeRequest(DisputeReason Reason, string Description, string PhotoUrl);

public record UploadImageResponse
{
    public string Url { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}
