namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>Kết quả <c>POST /orders/{id}/payment</c> (03-API-CONTRACT.md §4.2).</summary>
public record OrderPaymentDto
{
    public OrderDto Order { get; init; } = default!;

    /// <summary>Chỉ có khi thanh toán thành công.</summary>
    public QrTokenDto? QrTicket { get; init; }
}

public record QrTokenDto
{
    public string Token { get; init; } = default!;

    public DateTime ExpiresAt { get; init; }
}
