using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Business.Features.Ordering.Validators;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FreshRescue.Tests.Ordering;

/// <summary>Huỷ đơn: khách (BR-13), cửa hàng (BR-16), cả đợt (BR-23), quá hạn thanh toán (FR-X03).</summary>
public class OrderCancelTests
{
    private static readonly DateTime _now = new(2026, 10, 14, 13, 0, 0, DateTimeKind.Utc);

    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _brandId = Guid.NewGuid();
    private readonly FreshRescueDbContext _context;
    private readonly InMemoryOrderRepository _orders;
    private readonly Store _store;
    private readonly SaleBatch _batch;
    private readonly Offer _offer;

    public OrderCancelTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));
        _orders = new InMemoryOrderRepository(_context);

        _store = new Store { BrandId = _brandId, Name = "Chi nhánh Nguyễn Huệ", AddressLine = "12 Nguyễn Huệ", Ward = "Bến Nghé", District = "Quận 1", City = "TP.HCM" };
        _batch = new SaleBatch
        {
            Store = _store,
            StoreId = _store.Id,
            Status = SaleBatchStatus.Active,
            SaleStartTime = _now.AddHours(-1),
            SaleEndTime = _now.AddHours(1),
            PickupStartTime = _now.AddHours(1),
            PickupEndTime = _now.AddHours(3),
        };
        _offer = new Offer { SaleBatchId = _batch.Id, Name = "Túi mù bánh ngọt", ActualPrice = 35000, OriginalPrice = 105000, TotalQuantity = 10, RemainingQuantity = 5 };

        _context.AddRange(_store, _batch, _offer);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CancelByCustomer_DonChuaThanhToan_HuyVaTraTonKhoKhongHoanTienAsync()
    {
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2);

        var result = await CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
        result.CancelledBy.Should().Be(CancelledBy.Customer);
        result.RefundStatus.Should().Be(RefundStatus.None);
        result.CancelledAt.Should().Be(_now);
        (await RemainingAsync()).Should().Be(7);
    }

    [Fact]
    public async Task CancelByCustomer_DaThanhToanTrongHan_HoanTienThuHoiVeVaTraTonKhoAsync()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid, quantity: 1, paidAt: _now.AddMinutes(-5));

        var result = await CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
        result.RefundStatus.Should().Be(RefundStatus.Refunded);
        (await TicketStatusAsync(order.Id)).Should().Be(QrTicketStatus.Revoked);
        (await RemainingAsync()).Should().Be(6);
        (await _context.Notifications.AnyAsync()).Should().BeFalse();
    }

    // BR-13: hạn = min(PaidAt + 15', PickupStartTime − 5'), tính cả đầu mút.
    [Theory]
    [InlineData(-5, 60, 10, true)]
    [InlineData(-5, 60, 11, false)]
    [InlineData(-5, 8, 3, true)]
    [InlineData(-5, 8, 4, false)]
    public async Task CancelByCustomer_DaThanhToan_ChiHuyTrongCuaSoBr13Async(
        int paidMinutesAgo, int pickupStartInMinutes, int cancelAfterMinutes, bool allowed)
    {
        _batch.PickupStartTime = _now.AddMinutes(pickupStartInMinutes);
        await _context.SaveChangesAsync();
        var order = await SeedOrderAsync(OrderStatus.Paid, quantity: 1, paidAt: _now.AddMinutes(paidMinutesAgo));

        var act = () => CreateCustomerService(now: _now.AddMinutes(cancelAfterMinutes))
            .CancelByCustomerAsync(order.Id, CancellationToken.None);

        if (allowed)
        {
            await act.Should().NotThrowAsync();
        }
        else
        {
            await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.CancelWindowPassed && e.StatusCode == 409);
            (await RemainingAsync()).Should().Be(5);
        }
    }

    // 02 §6.4: huỷ hai lần không được cộng tồn kho hai lần.
    [Fact]
    public async Task CancelByCustomer_HuyLanHai_Bao409VaKhongCongTonKhoThemAsync()
    {
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2);
        await CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        var act = () => CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.CancelWindowPassed);
        (await RemainingAsync()).Should().Be(7);
    }

    // 02 §6.4: tồn kho không bao giờ vượt TotalQuantity.
    [Fact]
    public async Task CancelByCustomer_HoanVuotTong_ChanTranOTotalQuantityAsync()
    {
        _offer.RemainingQuantity = 9;
        await _context.SaveChangesAsync();
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2);

        await CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        (await RemainingAsync()).Should().Be(10);
    }

    [Fact]
    public async Task CancelByCustomer_DonDaTraoHang_Bao409Async()
    {
        var order = await SeedOrderAsync(OrderStatus.Redeemed, quantity: 1, paidAt: _now.AddMinutes(-5));

        var act = () => CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.OrderAlreadyRedeemed);
    }

    [Fact]
    public async Task CancelByCustomer_DonCuaNguoiKhac_Bao404Async()
    {
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 1, customerId: Guid.NewGuid());

        var act = () => CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound);
    }

    // Khách vừa thanh toán xong giữa lúc đọc và lúc huỷ: không được huỷ theo kiểu "chưa trả tiền".
    [Fact]
    public async Task CancelByCustomer_VuaThanhToanGiuaLucDocVaGhi_Bao409KhongDoiGiAsync()
    {
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2);
        _orders.BeforeUpdate = async () =>
        {
            var stored = await _context.Orders.SingleAsync(o => o.Id == order.Id);
            stored.Status = OrderStatus.Paid;
            await _context.SaveChangesAsync();
        };

        var act = () => CreateCustomerService().CancelByCustomerAsync(order.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ConcurrencyConflict);
        (await RemainingAsync()).Should().Be(5);
    }

    [Fact]
    public async Task CancelByStore_DonDaThanhToan_HoanTienVaBaoKhachAsync()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid, quantity: 1, paidAt: _now.AddMinutes(-30));

        var result = await CreateStaffService().CancelByStoreAsync(order.Id, new CancelOrderByStoreRequest("  Hết bánh do lò hỏng  "), CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
        result.CancelledBy.Should().Be(CancelledBy.Store);
        result.CancelReason.Should().Be("Hết bánh do lò hỏng");
        result.RefundStatus.Should().Be(RefundStatus.Refunded);
        (await TicketStatusAsync(order.Id)).Should().Be(QrTicketStatus.Revoked);

        var notification = await _context.Notifications.SingleAsync();
        notification.UserId.Should().Be(_customerId);
        notification.Type.Should().Be(NotificationType.OrderCancelledByStore);
        notification.RelatedEntityId.Should().Be(order.Id);
        notification.Body.Should().Contain("Hết bánh do lò hỏng");
    }

    [Fact]
    public async Task CancelByStore_DonChuaThanhToan_Bao409Async()
    {
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 1);

        var act = () => CreateStaffService().CancelByStoreAsync(order.Id, new CancelOrderByStoreRequest("Hết hàng"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.CancelWindowPassed);
        (await RemainingAsync()).Should().Be(5);
    }

    // BR-10
    [Fact]
    public async Task CancelByStore_ChiNhanhKhac_Bao403Async()
    {
        var order = await SeedOrderAsync(OrderStatus.Paid, quantity: 1, paidAt: _now.AddMinutes(-30));

        var act = () => CreateService([AppRoles.StoreStaff], storeId: Guid.NewGuid())
            .CancelByStoreAsync(order.Id, new CancelOrderByStoreRequest("Hết hàng"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Hết hàng", true)]
    public void StoreCancelValidator_LyDoBatBuoc(string reason, bool valid)
    {
        new CancelOrderByStoreRequestValidator().Validate(new CancelOrderByStoreRequest(reason)).IsValid.Should().Be(valid);
    }

    [Fact]
    public void StoreCancelValidator_LyDoQua500KyTu_KhongHopLe()
    {
        new CancelOrderByStoreRequestValidator().Validate(new CancelOrderByStoreRequest(new string('x', 501))).IsValid.Should().BeFalse();
    }

    // BR-23
    [Fact]
    public async Task CancelBatchOrders_HuyDonChoVaDonDaTra_ChiBaoKhachDaTraAsync()
    {
        var pending = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2);
        var paid = await SeedOrderAsync(OrderStatus.Paid, quantity: 1, paidAt: _now.AddMinutes(-30));
        var alreadyCancelled = await SeedOrderAsync(OrderStatus.Cancelled, quantity: 3);

        var count = await RunInTransactionAsync(s => s.CancelBatchOrdersAsync(_batch.Id, "Mất điện cả khu", CancellationToken.None));

        count.Should().Be(2);
        var orders = await _context.Orders.AsNoTracking().ToDictionaryAsync(o => o.Id);
        orders[pending.Id].Should().Match<Order>(o => o.Status == OrderStatus.Cancelled && o.CancelledBy == CancelledBy.Store && o.RefundStatus == RefundStatus.None);
        orders[paid.Id].Should().Match<Order>(o => o.Status == OrderStatus.Cancelled && o.RefundStatus == RefundStatus.Refunded && o.CancelReason == "Mất điện cả khu");
        orders[alreadyCancelled.Id].CancelReason.Should().BeNull();
        (await RemainingAsync()).Should().Be(8);
        (await _context.Notifications.SingleAsync()).RelatedEntityId.Should().Be(paid.Id);
    }

    [Fact]
    public async Task CancelBatchOrders_DaCoDonTraoHang_Bao409KhongHuyGiAsync()
    {
        var pending = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 1);
        await SeedOrderAsync(OrderStatus.Redeemed, quantity: 1, paidAt: _now.AddMinutes(-30));

        var act = () => RunInTransactionAsync(s => s.CancelBatchOrdersAsync(_batch.Id, "Mất điện", CancellationToken.None));

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchHasRedeemedOrders);
        (await _context.Orders.AsNoTracking().SingleAsync(o => o.Id == pending.Id)).Status.Should().Be(OrderStatus.PendingPayment);
    }

    // FR-X03
    [Fact]
    public async Task ExpireOverdueOrders_ChiHuyDonQuaHanVaChayLaiKhongCongThemAsync()
    {
        var overdue = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2, paymentDeadline: _now.AddMinutes(-1));
        var stillInTime = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 1, paymentDeadline: _now.AddMinutes(5));

        var first = await CreateCustomerService().ExpireOverdueOrdersAsync(CancellationToken.None);
        var second = await CreateCustomerService().ExpireOverdueOrdersAsync(CancellationToken.None);

        first.Should().Be(1);
        second.Should().Be(0);
        var orders = await _context.Orders.AsNoTracking().ToDictionaryAsync(o => o.Id);
        orders[overdue.Id].Should().Match<Order>(o => o.Status == OrderStatus.Cancelled && o.CancelledBy == CancelledBy.System && o.RefundStatus == RefundStatus.None);
        orders[stillInTime.Id].Status.Should().Be(OrderStatus.PendingPayment);
        (await RemainingAsync()).Should().Be(7);
    }

    private async Task<int> RunInTransactionAsync(Func<OrderService, Task<int>> action)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var result = await action(CreateStaffService());
        await transaction.CommitAsync();
        return result;
    }

    private async Task<Order> SeedOrderAsync(
        OrderStatus status,
        int quantity,
        DateTime? paidAt = null,
        Guid? customerId = null,
        DateTime? paymentDeadline = null)
    {
        var order = new Order
        {
            OrderCode = "FR-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerUserId = customerId ?? _customerId,
            StoreId = _store.Id,
            SaleBatchId = _batch.Id,
            Status = status,
            PaidAt = paidAt,
            PaymentDeadline = paymentDeadline ?? _now.AddMinutes(5),
            TotalAmount = 35000 * quantity,
        };
        order.Items.Add(new OrderItem { OfferId = _offer.Id, Quantity = quantity, UnitPrice = 35000, LineTotal = 35000 * quantity, OfferNameSnapshot = _offer.Name });
        _context.Orders.Add(order);
        if (paidAt is not null)
        {
            _context.QrTickets.Add(new QrTicket { OrderId = order.Id, Token = "token", IssuedAt = paidAt.Value, ExpiresAt = _batch.PickupEndTime });
        }

        await _context.SaveChangesAsync();
        return order;
    }

    private async Task<int> RemainingAsync()
        => (await _context.Offers.AsNoTracking().SingleAsync(o => o.Id == _offer.Id)).RemainingQuantity;

    private async Task<QrTicketStatus> TicketStatusAsync(Guid orderId)
        => (await _context.QrTickets.AsNoTracking().SingleAsync(t => t.OrderId == orderId)).Status;

    private OrderService CreateCustomerService(DateTime? now = null)
        => CreateService([AppRoles.Customer], userId: _customerId, now: now);

    private OrderService CreateStaffService()
        => CreateService([AppRoles.StoreStaff], storeId: _store.Id);

    private OrderService CreateService(string[] roles, Guid? userId = null, Guid? storeId = null, DateTime? now = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.Setup(u => u.Roles).Returns(roles);
        currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(roles.Contains);
        currentUser.Setup(u => u.StoreId).Returns(storeId);

        // Mỗi service một context mới, như mỗi request một scope — tránh dùng lại entity đang theo dõi.
        _context.ChangeTracker.Clear();
        var real = new UnitOfWork(_context);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Orders).Returns(_orders);
        uow.Setup(u => u.Offers).Returns(new InMemoryOfferRepository(_context));
        uow.Setup(u => u.QrTickets).Returns(real.QrTickets);
        uow.Setup(u => u.Notifications).Returns(real.Notifications);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(real.SaveChangesAsync);
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(real.BeginTransactionAsync);

        return new OrderService(
            uow.Object,
            new MapperConfiguration(c => c.AddProfile<OrderingProfile>()).CreateMapper(),
            currentUser.Object,
            Mock.Of<IQrTicketClient>(),
            new FixedTimeProvider(now ?? _now),
            NullLogger<OrderService>.Instance,
            Options.Create(new OrderingOptions()));
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
