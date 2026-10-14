namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>Thân <c>POST /staff/orders/{id}/cancel</c> — lý do bắt buộc, gửi kèm thông báo cho khách (BR-16).</summary>
public record CancelOrderByStoreRequest(string Reason);
