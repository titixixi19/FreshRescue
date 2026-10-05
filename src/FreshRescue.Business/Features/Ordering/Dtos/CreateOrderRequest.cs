namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>Thân <c>POST /orders</c> (03-API-CONTRACT.md §4.1).</summary>
public record CreateOrderRequest(Guid SaleBatchId, List<CreateOrderItemRequest> Items);
