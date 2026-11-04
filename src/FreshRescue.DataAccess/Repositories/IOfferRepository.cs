using FreshRescue.Entities.Sales;

namespace FreshRescue.DataAccess.Repositories;

public interface IOfferRepository : IGenericRepository<Offer>
{
    /// <summary>
    /// Nạp và giữ khoá cập nhật (UPDLOCK, ROWLOCK) các offer cho tới hết giao dịch —
    /// phương án bi quan của 02-DOMAIN-MODEL.md §6.3. Phải gọi bên trong giao dịch.
    /// </summary>
    Task<List<Offer>> GetForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Cộng trả tồn kho khi huỷ đơn, chặn trần ở <c>TotalQuantity</c> (02-DOMAIN-MODEL.md §6.4).
    /// Chỉ gọi sau khi <c>IOrderRepository.TryCancelAsync</c> trả <c>true</c>. Phải gọi trong giao dịch.
    /// </summary>
    Task RestockAsync(IReadOnlyDictionary<Guid, int> quantities, CancellationToken cancellationToken);

    /// <summary>
    /// Đặt lại tổng số suất của một offer (FR-S03) và dịch <c>RemainingQuantity</c> theo cùng một
    /// lượng, trong một câu UPDATE. Trả <c>false</c> khi tổng mới thấp hơn số đã giữ chỗ / đã bán
    /// (kho sẽ âm) hoặc không có offer — không ghi gì trong cả hai trường hợp.
    /// </summary>
    Task<bool> TryAdjustTotalAsync(Guid offerId, int newTotalQuantity, CancellationToken cancellationToken);
}
