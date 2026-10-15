using AutoMapper;
using FluentAssertions;
using FreshRescue.API.BackgroundServices;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Sales;
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

public class NoShowServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 14, 18, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly InMemoryOrderRepository _orders;
    private readonly InMemoryOfferRepository _offers;
    private readonly Store _store;
    private readonly SaleBatch _batch;
    private readonly Offer _offer;

    public NoShowServiceTests()
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
            Name = "Chi nhánh Landmark 81",
            AddressLine = "720A Điện Biên Phủ",
            Ward = "Phường 22",
            District = "Bình Thạnh",
            City = "TP.HCM"
        };

        _batch = new SaleBatch
        {
            Store = _store,
            StoreId = _store.Id,
            Status = SaleBatchStatus.Active,
            SaleStartTime = _now.AddHours(-4),
            SaleEndTime = _now.AddHours(-2),
            PickupStartTime = _now.AddHours(-2),
            PickupEndTime = _now.AddMinutes(-30), // Đã hết hạn nhận hàng 30 phút trước
        };

        _offer = new Offer
        {
            SaleBatchId = _batch.Id,
            Name = "Hộp sushi tươi",
            ActualPrice = 60000,
            OriginalPrice = 180000,
            TotalQuantity = 10,
            RemainingQuantity = 2
        };

        _context.AddRange(_store, _batch, _offer);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ProcessNoShows_DonDaThanhToanSauPickupEndTime_ChuyenSangNoShowKhongHoanTienAsync()
    {
        // Arrange: đơn đã thanh toán (Paid), khách không đến nhận hàng
        var order = await SeedOrderAsync(OrderStatus.Paid);
        var orderService = CreateOrderService(_now);

        // Act
        var count = await orderService.ProcessNoShowsAsync(CancellationToken.None);

        // Assert
        count.Should().Be(1);
        var updated = await _context.Orders.FindAsync(order.Id);
        updated!.Status.Should().Be(OrderStatus.NoShow);
        updated.RefundStatus.Should().Be(RefundStatus.None, "Theo BR-19: NoShow không hoàn tiền vì cửa hàng đã giữ đồ");
        updated.UpdatedAt.Should().Be(_now);
    }

    [Fact]
    public async Task ProcessNoShows_ChuaQuaPickupEndTime_KhongBiChuyenSangNoShowAsync()
    {
        // Arrange: đợt nhận hàng còn hạn 30 phút
        _batch.PickupEndTime = _now.AddMinutes(30);
        await _context.SaveChangesAsync();

        var order = await SeedOrderAsync(OrderStatus.Paid);
        var orderService = CreateOrderService(_now);

        // Act
        var count = await orderService.ProcessNoShowsAsync(CancellationToken.None);

        // Assert
        count.Should().Be(0);
        var updated = await _context.Orders.FindAsync(order.Id);
        updated!.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task ProcessNoShows_DonDaRedeemedHoacCancelled_KhongBiThayDoiAsync()
    {
        // Arrange: đơn đã nhận hàng và đơn đã bị huỷ
        var redeemedOrder = await SeedOrderAsync(OrderStatus.Redeemed);
        var cancelledOrder = await SeedOrderAsync(OrderStatus.Cancelled);

        var orderService = CreateOrderService(_now);

        // Act
        var count = await orderService.ProcessNoShowsAsync(CancellationToken.None);

        // Assert
        count.Should().Be(0);

        var updatedRedeemed = await _context.Orders.FindAsync(redeemedOrder.Id);
        updatedRedeemed!.Status.Should().Be(OrderStatus.Redeemed);

        var updatedCancelled = await _context.Orders.FindAsync(cancelledOrder.Id);
        updatedCancelled!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task NoShowService_HostedService_ProcessNoShowsAsync_ChayThanhCongAsync()
    {
        // Arrange
        var order = await SeedOrderAsync(OrderStatus.Paid);

        var clock = new MutableTimeProvider(_now);
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var orderService = CreateOrderService(_now);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOrderService))).Returns(orderService);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var hostedService = new NoShowService(
            scopeFactoryMock.Object,
            clock,
            NullLogger<NoShowService>.Instance);

        // Act
        await hostedService.ProcessNoShowsAsync(CancellationToken.None);

        // Assert
        var updated = await _context.Orders.FindAsync(order.Id);
        updated!.Status.Should().Be(OrderStatus.NoShow);
    }

    [Fact]
    public async Task CloseDueBatches_BaoDamXuLyNoShowTruocKhiDongDotAsync()
    {
        // Quy tắc 01-REQUIREMENTS.md §6.5 & T4-HIEU-03:
        // Đánh dấu NoShow phải chạy TRƯỚC KHI đóng đợt, nếu không sẽ không còn đơn nào ở trạng thái hợp lệ.
        var paidOrder = await SeedOrderAsync(OrderStatus.Paid);

        var uow = new UnitOfWork(_context);
        var orderService = CreateOrderService(_now);
        var saleBatchService = new SaleBatchService(
            uow,
            orderService,
            Mock.Of<AutoMapper.IMapper>(),
            Mock.Of<FreshRescue.Business.Common.ICurrentUser>(),
            new MutableTimeProvider(_now),
            NullLogger<SaleBatchService>.Instance);

        // Act: Đóng đợt bán đã tới PickupEndTime
        var closedCount = await saleBatchService.CloseDueBatchesAsync(CancellationToken.None);

        // Assert
        closedCount.Should().Be(1);

        // Kiểm tra đơn đã thành NoShow
        var updatedOrder = await _context.Orders.FindAsync(paidOrder.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.NoShow);

        // Kiểm tra đợt đã đóng
        var updatedBatch = await _context.SaleBatches.FindAsync(_batch.Id);
        updatedBatch!.Status.Should().Be(SaleBatchStatus.Closed);
    }

    private async Task<Order> SeedOrderAsync(OrderStatus status)
    {
        var order = new Order
        {
            OrderCode = "FR-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerUserId = Guid.NewGuid(),
            StoreId = _store.Id,
            SaleBatchId = _batch.Id,
            Status = status,
            PaymentDeadline = _now.AddHours(-3),
            TotalAmount = 60000,
            PlatformFee = 6000,
            StorePayoutAmount = 54000,
            RefundStatus = RefundStatus.None,
            Items =
            [
                new OrderItem
                {
                    OfferId = _offer.Id,
                    Quantity = 1,
                    UnitPrice = 60000,
                    LineTotal = 60000,
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
