using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Engagement;

public class NotificationService : INotificationService
{
    private static readonly TimeSpan _reminderWindow = TimeSpan.FromMinutes(30);

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider clock,
        IMapper mapper,
        ILogger<NotificationService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(
        NotificationListQuery query, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var q = _uow.Notifications.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (query.UnreadOnly == true)
        {
            q = q.Where(n => !n.IsRead);
        }

        q = q.OrderByDescending(n => n.CreatedAt);

        return q.ToPagedResponseAsync<Notification, NotificationDto>(page, pageSize, _mapper.ConfigurationProvider, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var notification = await _uow.Notifications.Query()
            .SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy thông báo.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();

        var unread = await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return 0;
        }

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Người dùng {UserId} đã đánh dấu {Count} thông báo đã đọc.", userId, unread.Count);
        return unread.Count;
    }

    public async Task<int> CreatePickupRemindersAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var threshold = now + _reminderWindow;

        // Quét các đơn đã thanh toán (Paid) mà thời gian nhận hàng còn lại <= 30 phút và chưa hết giờ
        var dueOrders = await _uow.Orders.Query()
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.SaleBatch)
            .Where(o => o.Status == OrderStatus.Paid
                        && o.SaleBatch.PickupEndTime <= threshold
                        && o.SaleBatch.PickupEndTime > now)
            .ToListAsync(cancellationToken);

        if (dueOrders.Count == 0)
        {
            return 0;
        }

        var orderIds = dueOrders.Select(o => o.Id).ToList();

        // Lấy danh sách OrderId đã được nhắc trước đó để tránh gửi trùng
        var alreadyRemindedOrderIds = await _uow.Notifications.Query()
            .Where(n => n.Type == NotificationType.PickupReminder && n.RelatedEntityId.HasValue && orderIds.Contains(n.RelatedEntityId.Value))
            .Select(n => n.RelatedEntityId!.Value)
            .ToListAsync(cancellationToken);

        var createdCount = 0;
        foreach (var order in dueOrders)
        {
            if (alreadyRemindedOrderIds.Contains(order.Id))
            {
                continue;
            }

            var storeName = order.Store?.Name ?? "cửa hàng";
            var pickupEndVn = order.SaleBatch.PickupEndTime.AddHours(7);

            var notification = new Notification
            {
                UserId = order.CustomerUserId,
                Title = "Nhắc nhở nhận hàng",
                Body = $"Đơn hàng {order.OrderCode} tại {storeName} sắp hết hạn nhận hàng. Vui lòng đến nhận trước {pickupEndVn:HH:mm}.",
                Type = NotificationType.PickupReminder,
                RelatedEntityId = order.Id,
                IsRead = false,
                CreatedAt = now,
            };

            await _uow.Notifications.AddAsync(notification, cancellationToken);
            createdCount++;
        }

        if (createdCount > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã tạo {Count} thông báo nhắc nhận hàng.", createdCount);
        }

        return createdCount;
    }

    public async Task<int> NotifyNewSaleBatchAsync(
        Guid saleBatchId, Guid storeId, string storeName, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        // OQ-29: Thông báo đợt bán mới chỉ gửi cho khách hàng đã bấm yêu thích chi nhánh
        var followerUserIds = await _uow.FavoriteStores.Query()
            .Where(f => f.StoreId == storeId)
            .Select(f => f.CustomerUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (followerUserIds.Count == 0)
        {
            return 0;
        }

        var storeDisplayName = storeName.StartsWith("Chi nhánh", StringComparison.OrdinalIgnoreCase) ? storeName : $"Chi nhánh {storeName}";
        foreach (var userId in followerUserIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = $"Đợt bán mới tại {storeName}",
                Body = $"{storeDisplayName} vừa mở đợt bán mới. Hãy vào xem ngay các túi giải cứu tươi ngon hôm nay!",
                Type = NotificationType.NewSaleBatch,
                RelatedEntityId = saleBatchId,
                IsRead = false,
                CreatedAt = now,
            };

            await _uow.Notifications.AddAsync(notification, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã gửi thông báo đợt bán mới tới {Count} khách theo dõi chi nhánh {StoreId} (OQ-29).", followerUserIds.Count, storeId);
        return followerUserIds.Count;
    }

    public async Task<NotificationDto> SendNotificationAsync(
        Guid userId, string title, string body, NotificationType type, Guid? relatedEntityId, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = now,
        };

        await _uow.Notifications.AddAsync(notification, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return _mapper.Map<NotificationDto>(notification);
    }

    private Guid RequireUserId()
    {
        return _currentUser.UserId ?? throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Vui lòng đăng nhập.");
    }
}
