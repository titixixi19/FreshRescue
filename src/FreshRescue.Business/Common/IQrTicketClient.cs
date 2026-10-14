namespace FreshRescue.Business.Common;

/// <summary>
/// Cấp và kiểm vé QR qua QrService (gRPC). Chỉ QrService giữ khoá ký (OQ-22), nên Business
/// không tự ký được — tầng API cài đặt interface này bằng gRPC client.
/// </summary>
public interface IQrTicketClient
{
    Task<string> IssueAsync(Guid orderId, Guid storeId, DateTime expiresAt, CancellationToken cancellationToken);

    /// <summary>Chỉ kiểm chữ ký và hạn dùng (03-API-CONTRACT.md §11).</summary>
    Task<QrTicketVerification> VerifyAsync(string token, CancellationToken cancellationToken);
}

/// <summary>
/// Kết quả kiểm vé. <c>ErrorCode</c> là <c>QR_INVALID_SIGNATURE</c> hoặc <c>QR_EXPIRED</c> khi không hợp lệ.
/// </summary>
public record QrTicketVerification(bool Valid, Guid? OrderId, Guid? StoreId, string? ErrorCode);
