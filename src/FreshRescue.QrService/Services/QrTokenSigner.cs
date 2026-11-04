using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.QrService.Services;

/// <summary>
/// Ký và kiểm token QR theo 02-DOMAIN-MODEL.md §6.5:
/// <c>token = base64url(payload) + "." + base64url(HMAC-SHA256(payload, khoá))</c>.
/// Token tự chứa dữ liệu (NFR-04) nên kiểm được mà không cần CSDL.
/// </summary>
public class QrTokenSigner
{
    public const string ErrorInvalidSignature = "QR_INVALID_SIGNATURE";
    public const string ErrorExpired = "QR_EXPIRED";

    private const int MinKeyLength = 32;

    // Payload viết camelCase đúng như 02 §6.5: { orderId, storeId, exp }.
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly byte[] _key;
    private readonly TimeProvider _clock;

    public QrTokenSigner(IConfiguration configuration, TimeProvider clock)
    {
        var key = configuration["QrSigning:Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < MinKeyLength)
        {
            throw new InvalidOperationException(
                "Cấu hình QrSigning:Key bị thiếu, rỗng hoặc ngắn hơn 32 ký tự. "
                + "Hãy chép mục 'QrSigning' từ appsettings.Development.json.example vào appsettings.Development.json.");
        }

        _key = Encoding.UTF8.GetBytes(key);
        _clock = clock;
    }

    public string Sign(Guid orderId, Guid storeId, long expiresAtUnix)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new QrPayload(orderId, storeId, expiresAtUnix), _json);
        var encodedPayload = WebEncoders.Base64UrlEncode(payload);
        return encodedPayload + "." + WebEncoders.Base64UrlEncode(ComputeSignature(encodedPayload));
    }

    public QrVerification Verify(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return QrVerification.Fail(ErrorInvalidSignature);
        }

        QrPayload? payload;
        try
        {
            // So sánh thời gian hằng: dừng sớm ở byte sai đầu tiên sẽ lộ chữ ký đúng qua thời gian phản hồi.
            var signature = WebEncoders.Base64UrlDecode(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(ComputeSignature(parts[0]), signature))
            {
                return QrVerification.Fail(ErrorInvalidSignature);
            }

            payload = JsonSerializer.Deserialize<QrPayload>(WebEncoders.Base64UrlDecode(parts[0]), _json);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return QrVerification.Fail(ErrorInvalidSignature);
        }

        if (payload is null)
        {
            return QrVerification.Fail(ErrorInvalidSignature);
        }

        if (_clock.GetUtcNow().ToUnixTimeSeconds() > payload.Exp)
        {
            return QrVerification.Fail(ErrorExpired, payload);
        }

        return new QrVerification(true, payload, null);
    }

    private byte[] ComputeSignature(string encodedPayload)
        => HMACSHA256.HashData(_key, Encoding.ASCII.GetBytes(encodedPayload));
}

public record QrPayload(Guid OrderId, Guid StoreId, long Exp);

public record QrVerification(bool Valid, QrPayload? Payload, string? ErrorCode)
{
    public static QrVerification Fail(string errorCode, QrPayload? payload = null) => new(false, payload, errorCode);
}
