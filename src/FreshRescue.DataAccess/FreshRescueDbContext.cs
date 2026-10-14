using System.Linq.Expressions;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Common;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FreshRescue.DataAccess;

/// <summary>
/// DbContext duy nhất của hệ thống. Chủ sở hữu: Tùng (07-WORK-BREAKDOWN.md §12.1).
/// Người khác không thêm <c>DbSet</c> vào đây — cấu hình từng entity đặt ở <c>Configurations/</c>.
/// Bảng <c>AppUser</c> là <c>Users</c> kế thừa từ Identity.
/// </summary>
public class FreshRescueDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    private readonly TimeProvider _clock;

    public FreshRescueDbContext(DbContextOptions<FreshRescueDbContext> options, TimeProvider clock)
        : base(options)
    {
        _clock = clock;
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<StoreStaff> StoreStaff => Set<StoreStaff>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<OfferTemplate> OfferTemplates => Set<OfferTemplate>();

    public DbSet<SaleBatch> SaleBatches => Set<SaleBatch>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<QrTicket> QrTickets => Set<QrTicket>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Dispute> Disputes => Set<Dispute>();

    public DbSet<FavoriteStore> FavoriteStores => Set<FavoriteStore>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Cố ý chỉ lọc 5 bảng ISoftDelete, không lọc bảng phụ thuộc (02 §8.4): đơn của chi nhánh
        // đã xoá vẫn phải còn. Hệ quả EF cảnh báo đã được xử lý bằng IgnoreQueryFilters (BR-22).
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Mặc định của EF là decimal(18,2) — tiền VND sẽ mọc hai số lẻ vô nghĩa (02 §8).
        // Toạ độ, điểm đánh giá, phần trăm tự khai lại độ chính xác trong file cấu hình.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 0);

        // SQL Server không lưu Kind: đọc lên là Unspecified, ra JSON thiếu hậu tố Z (OQ-33).
        // Mọi giá trị trong CSDL đã là UTC, nên chỉ cần gắn lại Kind lúc đọc.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mỗi entity một file cấu hình riêng, để không ai phải sửa file này khi thêm bảng.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FreshRescueDbContext).Assembly);

        ApplySoftDeleteFilters(modelBuilder);
        RestrictDeletes(modelBuilder);
    }

    // Khai một chỗ để không bảng ISoftDelete nào bị quên filter (OQ-06, BR-22).
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        var softDeleteTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(ISoftDelete).IsAssignableFrom(t.ClrType));

        foreach (var entityType in softDeleteTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var notDeleted = Expression.Not(Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)));
            entityType.SetQueryFilter(Expression.Lambda(notDeleted, parameter));
        }
    }

    // Hệ thống không xoá cứng dữ liệu nghiệp vụ (OQ-06), nên cascade chỉ có hại: xoá nhầm một
    // dòng kéo theo cả lịch sử đơn, và SQL Server từ chối tạo bảng khi có nhiều đường cascade
    // (Order → Store và Order → SaleBatch → Store). Bảng của Identity giữ nguyên mặc định.
    private static void RestrictDeletes(ModelBuilder modelBuilder)
    {
        var foreignKeys = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.ClrType.Namespace?.StartsWith("FreshRescue.Entities", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetForeignKeys());

        foreach (var foreignKey in foreignKeys)
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
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

        // Remove() trên bảng ISoftDelete thành xoá mềm (OQ-06) — service không phải nhớ, và không
        // ai lỡ tay xoá cứng làm đơn cũ trỏ vào khoảng không. BR-24 vẫn phải kiểm ở service.
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IHasCreatedAt>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
