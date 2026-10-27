using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Engagement;

/// <summary>
/// Quản lý thông báo trong ứng dụng (FR-C13, FR-X05, OQ-29).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại (hỗ trợ lọc chưa đọc, phân trang).
    /// </summary>
    Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(NotificationListQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc.
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken);

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc.
    /// </summary>
    Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// FR-X05: Quét các đơn Paid có khung nhận hàng kết thúc trong vòng 30 phút để tạo thông báo nhắc nhở.
    /// </summary>
    Task<int> CreatePickupRemindersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// OQ-29: Gửi thông báo có đợt bán mới tới tất cả khách hàng đã yêu thích chi nhánh.
    /// </summary>
    Task<int> NotifyNewSaleBatchAsync(Guid saleBatchId, Guid storeId, string storeName, CancellationToken cancellationToken);

    /// <summary>
    /// Gửi một thông báo tới người dùng cụ thể.
    /// </summary>
    Task<NotificationDto> SendNotificationAsync(Guid userId, string title, string body, NotificationType type, Guid? relatedEntityId, CancellationToken cancellationToken);
}
