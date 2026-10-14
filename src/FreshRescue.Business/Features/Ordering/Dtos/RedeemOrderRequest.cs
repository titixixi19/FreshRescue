namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>
/// Thân <c>POST /staff/redemptions</c> (03-API-CONTRACT.md §6.3): gửi đúng một trong hai.
/// <c>OrderCode</c> dùng khi khách hết pin hoặc webcam lỗi (FR-S06).
/// </summary>
public record RedeemOrderRequest(string? Token, string? OrderCode);
