using FreshRescue.QrService.Protos;
using Grpc.Core;

namespace FreshRescue.QrService.Services;

/// <summary>
/// T4-TV6-01. Ký và kiểm token HMAC-SHA256 theo 02-DOMAIN-MODEL.md §6.5.
/// Chỉ kiểm chữ ký và hạn dùng — phạm vi chi nhánh, khung giờ, idempotent do API lo.
/// </summary>
public class QrTicketService : Protos.QrService.QrServiceBase
{
    private readonly QrTokenSigner _signer;

    public QrTicketService(QrTokenSigner signer)
    {
        _signer = signer;
    }

    public override Task<IssueTicketReply> IssueTicket(IssueTicketRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId) || !Guid.TryParse(request.StoreId, out var storeId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "order_id và store_id phải là GUID."));
        }

        return Task.FromResult(new IssueTicketReply { Token = _signer.Sign(orderId, storeId, request.ExpiresAtUnix) });
    }

    public override Task<VerifyTicketReply> VerifyTicket(VerifyTicketRequest request, ServerCallContext context)
    {
        var result = _signer.Verify(request.Token);

        var reply = new VerifyTicketReply { Valid = result.Valid, ErrorCode = result.ErrorCode ?? string.Empty };
        if (result.Payload is not null)
        {
            reply.OrderId = result.Payload.OrderId.ToString();
            reply.StoreId = result.Payload.StoreId.ToString();
            reply.ExpiresAtUnix = result.Payload.Exp;
        }

        return Task.FromResult(reply);
    }
}
