using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess.Repositories;

/// <summary>
/// BR-12/OQ-30: do not read RatingAverage into C# and then write it back.
/// ExecuteUpdateAsync generates ONE SQL UPDATE and uses the original values
/// for both expressions, preventing lost updates when reviews arrive concurrently.
/// </summary>
public sealed class StoreRatingRepository : IStoreRatingRepository
{
    private readonly FreshRescueDbContext _context;

    public StoreRatingRepository(FreshRescueDbContext context)
    {
        _context = context;
    }

    public Task<int> AddRatingAsync(Guid storeId, byte rating, CancellationToken cancellationToken)
        => _context.Stores
            .IgnoreQueryFilters() // A redeemed order may belong to a later soft-deleted store.
            .Where(store => store.Id == storeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    store => store.RatingAverage,
                    store => (store.RatingAverage * store.RatingCount + rating)
                        / (store.RatingCount + 1))
                .SetProperty(store => store.RatingCount, store => store.RatingCount + 1),
                cancellationToken);
}
