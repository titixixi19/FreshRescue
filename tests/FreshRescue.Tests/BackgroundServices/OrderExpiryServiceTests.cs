using AutoMapper;
using FluentAssertions;
using FreshRescue.API.BackgroundServices;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using FreshRescue.Tests.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FreshRescue.Tests.BackgroundServices;

public class OrderExpiryServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 14, 14, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly InMemoryOrderRepository _orders;
    private readonly InMemoryOfferRepository _offers;
    private readonly Store _store;
    private readonly SaleBatch _batch;
    private readonly Offer _offer;

    public OrderExpiryServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new FreshRescueDbContext(options, new MutableTimeProvider(_now));
        _orders = new InMemoryOrderRepository(_context);
        _offers = new InMemoryOfferRepository(_context);

        _store = new Store
        {
            BrandId = Guid.NewGuid(),
            Name = "Chi nhánh Nguyễn Huệ",
            AddressLine = "12 Nguyễn Huệ",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM"
        };

        _batch = new SaleBatch
        {
            Store = _store,
            StoreId = _store.Id,
            Status = SaleBatchStatus.Active,
            SaleStartTime = _now.AddHours(-2),
            SaleEndTime = _now.AddHours(1),
            PickupStartTime = _now.AddHours(1),
            PickupEndTime = _now.AddHours(3),
        };

        _offer = new Offer
        {
            SaleBatchId = _batch.Id,
            Name = "Túi mù bánh ngọt",
            ActualPrice = 35000,
            OriginalPrice = 105000,
            TotalQuantity = 10,
            RemainingQuantity = 5
        };

        _context.AddRange(_store, _batch, _offer);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ExpireOverdueOrders_DonQuaHan_HuyVaHoanTonKhoCoChotChanAsync()
    {
        // Arrange: đơn quá hạn thanh toán 5 phút trước
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 3, deadline: _now.AddMinutes(-5));
        var service = CreateOrderService(_now);

        // Act: chạy quét quá hạn
        var expiredCount = await service.ExpireOverdueOrdersAsync(CancellationToken.None);

        // Assert
        expiredCount.Should().Be(1);

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);
        updatedOrder.CancelledBy.Should().Be(CancelledBy.System);
        updatedOrder.RefundStatus.Should().Be(RefundStatus.None);

        var updatedOffer = await _context.Offers.FindAsync(_offer.Id);
        // Ban đầu 5, hoàn 3 -> 8
        updatedOffer!.RemainingQuantity.Should().Be(8);
    }

    [Fact]
    public async Task ExpireOverdueOrders_HoanTonKhoKhongVuotQuaTotalQuantityAsync()
    {
        // Arrange: ban đầu kho còn 9 / tổng 10. Hoàn 3 đơn thì chỉ được lên tối đa 10 (chốt chặn FR-X03, 02 §6.4)
        _offer.RemainingQuantity = 9;
        await _context.SaveChangesAsync();

        await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 3, deadline: _now.AddMinutes(-2));
        var service = CreateOrderService(_now);

        // Act
        var expiredCount = await service.ExpireOverdueOrdersAsync(CancellationToken.None);

        // Assert
        expiredCount.Should().Be(1);
        var updatedOffer = await _context.Offers.FindAsync(_offer.Id);
        updatedOffer!.RemainingQuantity.Should().Be(10, "Chốt chặn không cho phép RemainingQuantity vượt quá TotalQuantity");
    }

    [Fact]
    public async Task ExpireOverdueOrders_DonChuaQuaHan_KhongBiHuyAsync()
    {
        // Arrange: đơn còn hạn 5 phút nữa
        var order = await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2, deadline: _now.AddMinutes(5));
        var service = CreateOrderService(_now);

        // Act
        var expiredCount = await service.ExpireOverdueOrdersAsync(CancellationToken.None);

        // Assert
        expiredCount.Should().Be(0);
        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.PendingPayment);

        var updatedOffer = await _context.Offers.FindAsync(_offer.Id);
        updatedOffer!.RemainingQuantity.Should().Be(5);
    }

    [Fact]
    public async Task ExpireOverdueOrders_ChayLaiNhieuLan_KhongHoanTonHaiLanAsync()
    {
        // Arrange: đơn quá hạn
        await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2, deadline: _now.AddMinutes(-5));
        var service = CreateOrderService(_now);

        // Act: lần 1
        var count1 = await service.ExpireOverdueOrdersAsync(CancellationToken.None);
        // Lần 2: chạy lại
        var count2 = await service.ExpireOverdueOrdersAsync(CancellationToken.None);

        // Assert
        count1.Should().Be(1);
        count2.Should().Be(0);

        var updatedOffer = await _context.Offers.FindAsync(_offer.Id);
        updatedOffer!.RemainingQuantity.Should().Be(7, "Chỉ được hoàn 1 lần, không được cộng dồn khi job chạy lặp lại");
    }

    [Fact]
    public async Task OrderExpiryService_HostedService_ProcessExpiredOrdersAsync_GoiThanhCongAsync()
    {
        await SeedOrderAsync(OrderStatus.PendingPayment, quantity: 2, deadline: _now.AddMinutes(-5));

        var clock = new MutableTimeProvider(_now);
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var orderService = CreateOrderService(_now);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOrderService))).Returns(orderService);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var hostedService = new OrderExpiryService(
            scopeFactoryMock.Object,
            clock,
            NullLogger<OrderExpiryService>.Instance);

        // Act
        await hostedService.ProcessExpiredOrdersAsync(CancellationToken.None);

        // Assert
        var updatedOffer = await _context.Offers.FindAsync(_offer.Id);
        updatedOffer!.RemainingQuantity.Should().Be(7);
    }

    private async Task<Order> SeedOrderAsync(OrderStatus status, int quantity, DateTime deadline)
    {
        var order = new Order
        {
            OrderCode = "FR-TEST001",
            CustomerUserId = Guid.NewGuid(),
            StoreId = _store.Id,
            SaleBatchId = _batch.Id,
            Status = status,
            PaymentDeadline = deadline,
            TotalAmount = quantity * _offer.ActualPrice,
            PlatformFee = quantity * _offer.ActualPrice * 0.1m,
            StorePayoutAmount = quantity * _offer.ActualPrice * 0.9m,
            RefundStatus = RefundStatus.None,
            Items =
            [
                new OrderItem
                {
                    OfferId = _offer.Id,
                    Quantity = quantity,
                    UnitPrice = _offer.ActualPrice,
                    LineTotal = quantity * _offer.ActualPrice,
                    OfferNameSnapshot = _offer.Name
                }
            ]
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    private OrderService CreateOrderService(DateTime now)
    {
        _context.ChangeTracker.Clear();
        var real = new UnitOfWork(_context);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Orders).Returns(_orders);
        uow.Setup(u => u.Offers).Returns(_offers);
        uow.Setup(u => u.SaleBatches).Returns(real.SaleBatches);
        uow.Setup(u => u.Stores).Returns(real.Stores);
        uow.Setup(u => u.QrTickets).Returns(real.QrTickets);
        uow.Setup(u => u.Notifications).Returns(real.Notifications);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(real.SaveChangesAsync);
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(real.BeginTransactionAsync);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderingProfile>());
        var mapper = config.CreateMapper();

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.Customer)).Returns(true);
        currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var qrClient = new Mock<IQrTicketClient>();

        return new OrderService(
            uow.Object,
            mapper,
            currentUser.Object,
            qrClient.Object,
            new MutableTimeProvider(now),
            NullLogger<OrderService>.Instance,
            Options.Create(new OrderingOptions()));
    }

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }
}
