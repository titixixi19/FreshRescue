namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>Thân <c>PUT /staff/offers/{id}</c>. Số lượng chỉnh riêng qua <c>PATCH .../quantity</c>.</summary>
public record UpdateOfferRequest(decimal ActualPrice, int MaxPerCustomer);
