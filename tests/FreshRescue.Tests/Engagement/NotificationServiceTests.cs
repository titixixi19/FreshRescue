using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Tests.Engagement;

/// <summary>
/// Kiểm thử đơn vị tính năng Thông báo trong ứng dụng (FR-C13, FR-X05, OQ-29, T5-HIEU-02).
/// </summary>
public class NotificationServiceTests
{
    private readonly EngagementFixture _f = new();

    [Fact]
    public async Task GetMyNotificationsAsync_LayDanhSachTheoUserVaPhanTrangAsync()
    {
        // Arrange
        var n1 = new Notification
        {
            UserId = _f.CustomerId,
            Title = "Thông báo 1",
            Body = "Nội dung 1",
            Type = NotificationType.NewSaleBatch,
            IsRead = false,
            CreatedAt = EngagementFixture.Now.AddHours(-2),
        };
        var n2 = new Notification
        {
            UserId = _f.CustomerId,
            Title = "Thông báo 2",
            Body = "Nội dung 2",
            Type = NotificationType.PickupReminder,
            IsRead = true,
            CreatedAt = EngagementFixture.Now.AddHours(-1),
        };
        var nOther = new Notification
        {
            UserId = Guid.NewGuid(),
            Title = "Của người khác",
            Body = "Nội dung",
            Type = NotificationType.NewSaleBatch,
            CreatedAt = EngagementFixture.Now,
        };

        _f.Context.Notifications.AddRange(n1, n2, nOther);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var result = await service.GetMyNotificationsAsync(
            new NotificationListQuery { Page = 1, PageSize = 10 },
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        // Mới nhất trước: n2 trước n1
        result.Items[0].Id.Should().Be(n2.Id);
        result.Items[0].Title.Should().Be("Thông báo 2");
        result.Items[1].Id.Should().Be(n1.Id);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_LocUnreadOnly_ChiLayChuaDocAsync()
    {
        // Arrange
        var unread = new Notification
        {
            UserId = _f.CustomerId,
            Title = "Chưa đọc",
            Body = "Nội dung",
            Type = NotificationType.NewSaleBatch,
            IsRead = false,
            CreatedAt = EngagementFixture.Now,
        };
        var read = new Notification
        {
            UserId = _f.CustomerId,
            Title = "Đã đọc",
            Body = "Nội dung",
            Type = NotificationType.PickupReminder,
            IsRead = true,
            CreatedAt = EngagementFixture.Now.AddMinutes(-10),
        };

        _f.Context.Notifications.AddRange(unread, read);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var result = await service.GetMyNotificationsAsync(
            new NotificationListQuery { UnreadOnly = true },
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(unread.Id);
        result.Items[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_ThongBaoCuaUser_DanhDauThanhCongAsync()
    {
        // Arrange
        var notification = new Notification
        {
            UserId = _f.CustomerId,
            Title = "Chưa đọc",
            Body = "Nội dung",
            Type = NotificationType.NewSaleBatch,
            IsRead = false,
            CreatedAt = EngagementFixture.Now,
        };
        _f.Context.Notifications.Add(notification);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var result = await service.MarkAsReadAsync(notification.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var updated = await _f.Context.Notifications.FindAsync(notification.Id);
        updated!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_ThongBaoCuaUserKhac_NemNotFoundAsync()
    {
        // Arrange
        var notification = new Notification
        {
            UserId = Guid.NewGuid(), // Khác _f.CustomerId
            Title = "Chưa đọc",
            Body = "Nội dung",
            Type = NotificationType.NewSaleBatch,
            IsRead = false,
            CreatedAt = EngagementFixture.Now,
        };
        _f.Context.Notifications.Add(notification);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var act = () => service.MarkAsReadAsync(notification.Id, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_DanhDauTatCaChuaDocThanhDaDocAsync()
    {
        // Arrange
        var n1 = new Notification { UserId = _f.CustomerId, Title = "1", Body = "1", IsRead = false, CreatedAt = EngagementFixture.Now };
        var n2 = new Notification { UserId = _f.CustomerId, Title = "2", Body = "2", IsRead = false, CreatedAt = EngagementFixture.Now };
        var n3 = new Notification { UserId = _f.CustomerId, Title = "3", Body = "3", IsRead = true, CreatedAt = EngagementFixture.Now };

        _f.Context.Notifications.AddRange(n1, n2, n3);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var updatedCount = await service.MarkAllAsReadAsync(CancellationToken.None);

        // Assert
        updatedCount.Should().Be(2);
        var unreadCount = await _f.Context.Notifications.CountAsync(n => n.UserId == _f.CustomerId && !n.IsRead);
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task CreatePickupRemindersAsync_DonPaidConTrong30Phut_TaoThongBaoNhacNhoAsync()
    {
        // Arrange: Đợt bán có PickupEndTime còn 20 phút nữa (nằm trong khoảng 30 phút)
        var batch = new SaleBatch
        {
            StoreId = _f.Store.Id,
            Store = _f.Store,
            Status = SaleBatchStatus.Active,
            SaleStartTime = EngagementFixture.Now.AddHours(-2),
            SaleEndTime = EngagementFixture.Now.AddMinutes(-30),
            PickupStartTime = EngagementFixture.Now.AddMinutes(-30),
            PickupEndTime = EngagementFixture.Now.AddMinutes(20), // Còn 20 phút
        };

        var order = new Order
        {
            OrderCode = "FR-REMIND01",
            CustomerUserId = _f.CustomerId,
            StoreId = _f.Store.Id,
            Store = _f.Store,
            SaleBatch = batch,
            SaleBatchId = batch.Id,
            Status = OrderStatus.Paid,
            TotalAmount = 50000,
        };

        _f.Context.AddRange(batch, order);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var count = await service.CreatePickupRemindersAsync(CancellationToken.None);

        // Assert
        count.Should().Be(1);
        var notifications = await _f.Context.Notifications
            .Where(n => n.UserId == _f.CustomerId && n.Type == NotificationType.PickupReminder)
            .ToListAsync();

        notifications.Should().HaveCount(1);
        notifications[0].RelatedEntityId.Should().Be(order.Id);
        notifications[0].Title.Should().Contain("FR-X05");
        notifications[0].Body.Should().Contain(order.OrderCode);
        notifications[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePickupRemindersAsync_ChayLai_KhongTaoThongBaoTrungAsync()
    {
        // Arrange
        var batch = new SaleBatch
        {
            StoreId = _f.Store.Id,
            Store = _f.Store,
            Status = SaleBatchStatus.Active,
            SaleStartTime = EngagementFixture.Now.AddHours(-2),
            SaleEndTime = EngagementFixture.Now.AddMinutes(-30),
            PickupStartTime = EngagementFixture.Now.AddMinutes(-30),
            PickupEndTime = EngagementFixture.Now.AddMinutes(15),
        };

        var order = new Order
        {
            OrderCode = "FR-REMIND02",
            CustomerUserId = _f.CustomerId,
            StoreId = _f.Store.Id,
            Store = _f.Store,
            SaleBatch = batch,
            SaleBatchId = batch.Id,
            Status = OrderStatus.Paid,
            TotalAmount = 50000,
        };

        _f.Context.AddRange(batch, order);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Lần 1: Tạo nhắc nhở
        var count1 = await service.CreatePickupRemindersAsync(CancellationToken.None);
        count1.Should().Be(1);

        // Act: Lần 2 quét lại
        var count2 = await service.CreatePickupRemindersAsync(CancellationToken.None);

        // Assert: Không tạo thêm
        count2.Should().Be(0);
        var total = await _f.Context.Notifications.CountAsync(n => n.RelatedEntityId == order.Id);
        total.Should().Be(1);
    }

    [Fact]
    public async Task CreatePickupRemindersAsync_DonNgoaiKhung30Phut_KhongTaoThongBaoAsync()
    {
        // Arrange: Còn hơn 1 tiếng mới hết giờ nhận hàng
        var batch = new SaleBatch
        {
            StoreId = _f.Store.Id,
            Store = _f.Store,
            Status = SaleBatchStatus.Active,
            SaleStartTime = EngagementFixture.Now.AddHours(-2),
            SaleEndTime = EngagementFixture.Now.AddMinutes(-30),
            PickupStartTime = EngagementFixture.Now.AddMinutes(-30),
            PickupEndTime = EngagementFixture.Now.AddHours(2), // Còn 2 tiếng
        };

        var order = new Order
        {
            OrderCode = "FR-REMIND03",
            CustomerUserId = _f.CustomerId,
            StoreId = _f.Store.Id,
            Store = _f.Store,
            SaleBatch = batch,
            SaleBatchId = batch.Id,
            Status = OrderStatus.Paid,
            TotalAmount = 50000,
        };

        _f.Context.AddRange(batch, order);
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();

        // Act
        var count = await service.CreatePickupRemindersAsync(CancellationToken.None);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task NotifyNewSaleBatchAsync_ChiGuiKhachHangThichChiNhanh_OQ29Async()
    {
        // Arrange: 2 khách hàng, 1 người thích chi nhánh, 1 người không
        var followerId = _f.CustomerId;
        var nonFollowerId = Guid.NewGuid();

        _f.Context.FavoriteStores.Add(new FavoriteStore
        {
            CustomerUserId = followerId,
            StoreId = _f.Store.Id,
            CreatedAt = EngagementFixture.Now,
        });
        await _f.Context.SaveChangesAsync();

        var service = _f.NotificationService();
        var batchId = Guid.NewGuid();

        // Act
        var count = await service.NotifyNewSaleBatchAsync(batchId, _f.Store.Id, _f.Store.Name, CancellationToken.None);

        // Assert
        count.Should().Be(1);

        var followerNotif = await _f.Context.Notifications
            .SingleOrDefaultAsync(n => n.UserId == followerId && n.RelatedEntityId == batchId);
        followerNotif.Should().NotBeNull();
        followerNotif!.Type.Should().Be(NotificationType.NewSaleBatch);
        followerNotif.Title.Should().Contain(_f.Store.Name);

        var nonFollowerNotif = await _f.Context.Notifications
            .SingleOrDefaultAsync(n => n.UserId == nonFollowerId && n.RelatedEntityId == batchId);
        nonFollowerNotif.Should().BeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_TaoVaTraVeDtoThanhCongAsync()
    {
        // Arrange
        var service = _f.NotificationService();
        var targetUserId = Guid.NewGuid();

        // Act
        var result = await service.SendNotificationAsync(
            targetUserId,
            "Tiêu đề thử nghiệm",
            "Nội dung thử nghiệm",
            NotificationType.OrderCancelledByStore,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Tiêu đề thử nghiệm");
        result.Type.Should().Be(NotificationType.OrderCancelledByStore);
        result.IsRead.Should().BeFalse();

        var inDb = await _f.Context.Notifications.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be(targetUserId);
    }
}
