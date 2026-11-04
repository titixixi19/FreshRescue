using FreshRescue.Business.Features.Sales.Dtos;

namespace FreshRescue.Business.Features.Sales;

public interface IOfferService
{
    /// <summary>Thêm offer từ một mẫu vào đợt chưa đóng (BR-18, BR-07).</summary>
    Task<OfferDto> AddToBatchAsync(Guid saleBatchId, CreateSaleBatchOfferRequest request, CancellationToken cancellationToken);

    /// <summary>Đổi giá bán và hạn mức mỗi khách; giá bị khoá sau đơn Paid đầu tiên (OQ-14).</summary>
    Task<OfferDto> UpdateAsync(Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Đặt tổng số suất mới (FR-S03). Chỉ đổi <c>TotalQuantity</c>; phần còn lại được cộng trừ theo
    /// đúng chênh lệch. Không bao giờ để <c>RemainingQuantity</c> âm (BR-01).
    /// </summary>
    Task<OfferDto> AdjustQuantityAsync(Guid offerId, AdjustOfferQuantityRequest request, CancellationToken cancellationToken);

    /// <summary>Xoá mềm offer chưa có đơn (BR-24).</summary>
    Task DeleteAsync(Guid offerId, CancellationToken cancellationToken);
}
