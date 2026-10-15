namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>Thân <c>POST /staff/sale-batches/{id}/cancel</c> — lý do bắt buộc (BR-23).</summary>
public record CancelSaleBatchRequest(string Reason);
