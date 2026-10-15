namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>
/// Một dòng offer khi tạo đợt bán, cũng là thân của <c>POST /staff/sale-batches/{id}/offers</c>.
/// <paramref name="MaxPerCustomer"/> bỏ trống thì dùng mặc định 3 (BR-06).
/// </summary>
public record CreateSaleBatchOfferRequest(
    Guid OfferTemplateId,
    decimal ActualPrice,
    int TotalQuantity,
    int? MaxPerCustomer);
