using FreshRescue.Entities.Sales;

namespace FreshRescue.DataAccess.Repositories;

public interface IOfferRepository : IGenericRepository<Offer>
{
    /// <summary>
    /// Nạp và giữ khoá cập nhật (UPDLOCK, ROWLOCK) các offer cho tới hết giao dịch —
    /// phương án bi quan của 02-DOMAIN-MODEL.md §6.3. Phải gọi bên trong giao dịch.
    /// </summary>
    Task<List<Offer>> GetForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
