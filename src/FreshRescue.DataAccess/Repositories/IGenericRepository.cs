namespace FreshRescue.DataAccess.Repositories;

/// <summary>
/// Thao tác CRUD chung. Truy vấn đặc thù (khoá hàng, UPDATE có điều kiện…) đặt ở repository
/// riêng của entity đó, ví dụ <c>IOfferRepository</c>.
/// </summary>
public interface IGenericRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Truy vấn chưa thực thi, để service tự lọc / phân trang / chiếu sang DTO.</summary>
    IQueryable<TEntity> Query();

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Remove(TEntity entity);
}
