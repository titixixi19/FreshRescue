using FreshRescue.Business.Common;
using FreshRescue.QrService.Protos;

namespace FreshRescue.API.Extensions;

/// <summary>Cài đặt <see cref="IQrTicketClient"/> bằng gRPC client sinh từ qr.proto (03-API-CONTRACT.md §11).</summary>
public class GrpcQrTicketClient : IQrTicketClient
{
    private readonly QrService.Protos.QrService.QrServiceClient _client;

    public GrpcQrTicketClient(QrService.Protos.QrService.QrServiceClient client)
    {
        _client = client;
    }

    public async Task<string> IssueAsync(Guid orderId, Guid storeId, DateTime expiresAt, CancellationToken cancellationToken)
    {
        var reply = await _client.IssueTicketAsync(
            new IssueTicketRequest
            {
                OrderId = orderId.ToString(),
                StoreId = storeId.ToString(),
                ExpiresAtUnix = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            },
            cancellationToken: cancellationToken);

        return reply.Token;
    }

    public async Task<QrTicketVerification> VerifyAsync(string token, CancellationToken cancellationToken)
    {
        var reply = await _client.VerifyTicketAsync(new VerifyTicketRequest { Token = token }, cancellationToken: cancellationToken);

        return new QrTicketVerification(
            reply.Valid,
            Guid.TryParse(reply.OrderId, out var orderId) ? orderId : null,
            Guid.TryParse(reply.StoreId, out var storeId) ? storeId : null,
            string.IsNullOrEmpty(reply.ErrorCode) ? null : reply.ErrorCode);
    }
}
