using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class
{
    protected readonly FreshRescueDbContext Context;
    protected readonly DbSet<TEntity> Set;

    public GenericRepository(FreshRescueDbContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Set.FindAsync([id], cancellationToken);

    public IQueryable<TEntity> Query() => Set.AsQueryable();

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        => await Set.AddAsync(entity, cancellationToken);

    public void Remove(TEntity entity) => Set.Remove(entity);
}
