namespace FreshRescue.DataAccess.Repositories;

/// <summary>
/// Atomically increments a store's rating count and average in a single SQL UPDATE.
/// Returns the number of affected stores (normally 1).
/// </summary>
public interface IStoreRatingRepository
{
    Task<int> AddRatingAsync(Guid storeId, byte rating, CancellationToken cancellationToken);
}
