using FreshRescue.DataAccess.Repositories;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore.Storage;

namespace FreshRescue.DataAccess.UnitOfWork;

/// <summary>
/// Cửa duy nhất để service chạm CSDL — service không tiêm thẳng <c>DbContext</c>
/// (06-CODING-STANDARDS.md §7). Mọi repository dùng chung một <c>DbContext</c>, nên một
/// <see cref="SaveChangesAsync"/> lưu hết thay đổi của cả request.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Chỉ để đọc. Tạo user, đổi mật khẩu, gán role phải đi qua <c>UserManager</c> — nó lo băm
    /// mật khẩu và các cột Normalized* mà repository không biết.
    /// </summary>
    IGenericRepository<AppUser> Users { get; }

    IGenericRepository<RefreshToken> RefreshTokens { get; }

    IGenericRepository<Brand> Brands { get; }

    IGenericRepository<Store> Stores { get; }

    IGenericRepository<StoreStaff> StoreStaff { get; }

    IGenericRepository<Category> Categories { get; }

    IGenericRepository<OfferTemplate> OfferTemplates { get; }

    IGenericRepository<SaleBatch> SaleBatches { get; }

    IGenericRepository<Offer> Offers { get; }

    IGenericRepository<Order> Orders { get; }

    IGenericRepository<OrderItem> OrderItems { get; }

    IGenericRepository<QrTicket> QrTickets { get; }

    IGenericRepository<Payment> Payments { get; }

    IGenericRepository<Review> Reviews { get; }

    IGenericRepository<Dispute> Disputes { get; }

    /// <summary>
    /// Khoá chính tổ hợp (CustomerUserId, StoreId) — <c>GetByIdAsync</c> không dùng được,
    /// tìm bằng <c>Query()</c>.
    /// </summary>
    IGenericRepository<FavoriteStore> FavoriteStores { get; }

    IGenericRepository<Notification> Notifications { get; }

    /// <summary>Gọi đúng một lần ở cuối mỗi request (06-CODING-STANDARDS.md §4.2).</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Dùng cho đặt hàng, hoàn tồn kho, đối soát QR (02-DOMAIN-MODEL.md §6).</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Bỏ mọi entity đang theo dõi. Gọi trước khi thử lại sau xung đột đồng thời, để lần sau
    /// nạp lại dữ liệu mới từ CSDL thay vì dùng bản cũ đã lỗi thời (02-DOMAIN-MODEL.md §6.1).
    /// </summary>
    void ClearChangeTracker();
}
