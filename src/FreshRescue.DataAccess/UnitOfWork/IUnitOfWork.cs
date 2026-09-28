using Microsoft.EntityFrameworkCore.Storage;

namespace FreshRescue.DataAccess.UnitOfWork;

/// <summary>
/// Cửa duy nhất để service chạm CSDL — service không tiêm thẳng <c>DbContext</c>
/// (06-CODING-STANDARDS.md §7). Mỗi entity sẽ có một thuộc tính repository ở đây,
/// ví dụ <c>IGenericRepository&lt;SaleBatch&gt; SaleBatches</c>.
/// </summary>
public interface IUnitOfWork
{
    // TODO(Tung, 2026-09-29): thêm thuộc tính repository cho từng entity sau khi có entity.

    /// <summary>Gọi đúng một lần ở cuối mỗi request (06-CODING-STANDARDS.md §4.2).</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Dùng cho đặt hàng, hoàn tồn kho, đối soát QR (02-DOMAIN-MODEL.md §6).</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
