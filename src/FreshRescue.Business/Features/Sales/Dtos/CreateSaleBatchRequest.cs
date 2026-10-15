namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>
/// Thân <c>POST /staff/sale-batches</c> (03-API-CONTRACT.md §6.1). <paramref name="StoreId"/> chỉ
/// dùng khi Brand Manager tạo thay — Store Staff luôn lấy chi nhánh từ token.
/// </summary>
public record CreateSaleBatchRequest(
    string? Name,
    DateTime SaleStartTime,
    DateTime SaleEndTime,
    DateTime PickupStartTime,
    DateTime PickupEndTime,
    Guid? StoreId,
    List<CreateSaleBatchOfferRequest> Offers);
