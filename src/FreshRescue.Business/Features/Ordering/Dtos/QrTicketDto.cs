namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>
/// Vé QR của khách (03-API-CONTRACT.md §4.3). <c>OrderCode</c> để nhân viên nhập tay khi
/// webcam trục trặc (FR-S06).
/// </summary>
public record QrTicketDto
{
    public string OrderCode { get; init; } = default!;

    public string Token { get; init; } = default!;

    public DateTime ExpiresAt { get; init; }

    public DateTime PickupStartTime { get; init; }

    public string StoreName { get; init; } = default!;
}
