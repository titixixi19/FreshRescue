using Microsoft.EntityFrameworkCore.Storage;

namespace FreshRescue.DataAccess.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly FreshRescueDbContext _context;

    public UnitOfWork(FreshRescueDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => _context.Database.BeginTransactionAsync(cancellationToken);
}
