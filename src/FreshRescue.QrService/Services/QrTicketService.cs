namespace FreshRescue.QrService.Services;

/// <summary>
/// Chủ sở hữu: TV6 (T4-TV6-01). Ký và kiểm token HMAC-SHA256 theo 02-DOMAIN-MODEL.md §6.5.
/// Chỉ kiểm chữ ký và hạn dùng — phạm vi chi nhánh, khung giờ, idempotent do API lo.
/// Chưa override method nào thì gọi vào sẽ nhận <c>UNIMPLEMENTED</c>.
/// </summary>
public class QrTicketService : Protos.QrService.QrServiceBase
{
    // TODO(TV6, 2026-10-06): override IssueTicket, VerifyTicket.
}
