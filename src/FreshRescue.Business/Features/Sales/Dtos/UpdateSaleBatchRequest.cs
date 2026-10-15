namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>
/// Thân <c>PUT /staff/sale-batches/{id}</c>. Luôn gửi đủ bốn mốc; giá và số lượng của từng offer
/// sửa qua <c>/staff/offers/{id}</c>.
/// </summary>
public record UpdateSaleBatchRequest(
    string? Name,
    DateTime SaleStartTime,
    DateTime SaleEndTime,
    DateTime PickupStartTime,
    DateTime PickupEndTime);
