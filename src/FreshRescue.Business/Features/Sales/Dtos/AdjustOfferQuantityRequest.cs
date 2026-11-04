namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>Thân <c>PATCH /staff/offers/{id}/quantity</c> — đặt tổng số suất mới (FR-S03).</summary>
public record AdjustOfferQuantityRequest(int TotalQuantity);
