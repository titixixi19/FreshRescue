using FreshRescue.Business.Common;

namespace FreshRescue.Business.Seeding;

/// <summary>
/// Thay QrService khi seed. Chỉ QrService giữ khoá ký (OQ-22) nên seeder không ký thật được:
/// vé sinh ra có dạng <c>SEED.{orderId}.{storeId}</c>, đủ để chạy luồng thanh toán và đối soát trong seeder,
/// nhưng QrService thật sẽ từ chối. Đơn còn ở trạng thái Paid khi demo thì nhập mã đơn thủ công (OQ-32).
/// </summary>
public sealed class SeedQrTicketClient : IQrTicketClient
{
    public Task<string> IssueAsync(Guid orderId, Guid storeId, DateTime expiresAt, CancellationToken cancellationToken)
        => Task.FromResult($"SEED.{orderId:N}.{storeId:N}");

    public Task<QrTicketVerification> VerifyAsync(string token, CancellationToken cancellationToken)
    {
        var parts = token.Split('.');
        if (parts.Length == 3 && parts[0] == "SEED"
            && Guid.TryParseExact(parts[1], "N", out var orderId)
            && Guid.TryParseExact(parts[2], "N", out var storeId))
        {
            return Task.FromResult(new QrTicketVerification(true, orderId, storeId, null));
        }

        return Task.FromResult(new QrTicketVerification(false, null, null, ErrorCodes.QrInvalidSignature));
    }
}
