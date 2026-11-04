namespace FreshRescue.Business.Features.Ordering.Dtos;

public record CreateOrderItemRequest(Guid OfferId, int Quantity);
