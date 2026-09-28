using FreshRescue.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess;

/// <summary>
/// DbContext duy nhất của hệ thống. Chủ sở hữu: Tùng (07-WORK-BREAKDOWN.md §12.1).
/// Người khác không thêm <c>DbSet</c> vào đây — cấu hình từng entity đặt ở <c>Configurations/</c>.
/// </summary>
public class FreshRescueDbContext : DbContext
{
    private readonly TimeProvider _clock;

    public FreshRescueDbContext(DbContextOptions<FreshRescueDbContext> options, TimeProvider clock)
        : base(options)
    {
        _clock = clock;
    }

    // TODO(Tung, 2026-09-29): khai đủ 17 DbSet theo 02-DOMAIN-MODEL.md §3, chuyển base class sang
    // IdentityDbContext<AppUser, AppRole, Guid>, và thêm Global Query Filter cho ISoftDelete.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mỗi entity một file cấu hình riêng, để không ai phải sửa file này khi thêm bảng.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FreshRescueDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Gán tập trung ở đây để không service nào phải nhớ tự gán (02-DOMAIN-MODEL.md §1.1).
    private void StampAuditFields()
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
