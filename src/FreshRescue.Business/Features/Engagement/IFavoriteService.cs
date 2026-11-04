using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;

namespace FreshRescue.Business.Features.Engagement;

/// <summary>
/// Quản lý danh sách chi nhánh yêu thích của khách hàng (FR-C12).
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// Thêm chi nhánh vào danh sách yêu thích. Idempotent: nếu đã có thì không tạo trùng.
    /// </summary>
    Task<bool> AddFavoriteAsync(Guid storeId, CancellationToken cancellationToken);

    /// <summary>
    /// Bỏ chi nhánh khỏi danh sách yêu thích. Idempotent: nếu chưa có thì trả về thành công.
    /// </summary>
    Task<bool> RemoveFavoriteAsync(Guid storeId, CancellationToken cancellationToken);

    /// <summary>
    /// Lấy danh sách chi nhánh yêu thích của khách hàng hiện tại, phân trang.
    /// </summary>
    Task<PagedResponse<FavoriteStoreDto>> GetMyFavoritesAsync(int page, int pageSize, CancellationToken cancellationToken);
}
