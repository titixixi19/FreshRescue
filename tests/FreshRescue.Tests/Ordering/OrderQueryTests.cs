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
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FreshRescue.Tests.Ordering;

/// <summary>Lịch sử đơn của khách (FR-C09) và đơn phải trả trong đợt (FR-S04).</summary>
public class OrderQueryTests
{
    private static readonly DateTime _now = new(2026, 10, 14, 13, 0, 0, DateTimeKind.Utc);

    private readonly Guid _brandId = Guid.NewGuid();
    private readonly FreshRescueDbContext _context;
    private readonly AppUser _customer;
    private readonly Store _store;
    private readonly SaleBatch _batch;

    public OrderQueryTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));

        _customer = new AppUser { FullName = "Nguyễn Văn An", UserName = "an@example.com", Email = "an@example.com" };
        _store = new Store { BrandId = _brandId, Name = "Chi nhánh Nguyễn Huệ", AddressLine = "12 Nguyễn Huệ", Ward = "Bến Nghé", District = "Quận 1", City = "TP.HCM" };
        _batch = new SaleBatch
        {
            Store = _store,
            StoreId = _store.Id,
            Status = SaleBatchStatus.Active,
            SaleStartTime = _now.AddHours(-1),
            SaleEndTime = _now.AddHours(1),
            PickupStartTime = _now.AddMinutes(-30),
            PickupEndTime = _now.AddHours(2),
        };
        _context.AddRange(_customer, _store, _batch);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetMyOrders_ChiDonCuaToiMoiNhatTruocVaPhanTrangDungAsync()
    {
        var oldest = Seed(OrderStatus.Redeemed, createdMinutesAgo: 30);
        var middle = Seed(OrderStatus.Paid, createdMinutesAgo: 20);
        var newest = Seed(OrderStatus.Cancelled, createdMinutesAgo: 10);
        Seed(OrderStatus.Paid, createdMinutesAgo: 5, customerId: Guid.NewGuid());
        await SaveAsync();

        var page1 = await CreateCustomerService().GetMyOrdersAsync(new OrderListQuery { PageSize = 2 }, CancellationToken.None);
        var page2 = await CreateCustomerService().GetMyOrdersAsync(new OrderListQuery { Page = 2, PageSize = 2 }, CancellationToken.None);

        page1.Items.Select(o => o.Id).Should().Equal(newest.Id, middle.Id);
        page2.Items.Select(o => o.Id).Should().Equal(oldest.Id);
        page1.TotalCount.Should().Be(3);
        page1.TotalPages.Should().Be(2);
        page1.Items[0].StoreName.Should().Be("Chi nhánh Nguyễn Huệ");
        page1.Items[0].Items.Should().ContainSingle().Which.OfferName.Should().Be("Túi mù bánh ngọt");
    }

    [Fact]
    public async Task GetMyOrders_LocTheoStatusAsync()
    {
        var paid = Seed(OrderStatus.Paid);
        Seed(OrderStatus.Cancelled);
        await SaveAsync();

        var result = await CreateCustomerService().GetMyOrdersAsync(new OrderListQuery { Status = OrderStatus.Paid }, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(paid.Id);
    }

    // BR-22: chi nhánh bị xoá mềm sau khi khách đặt, đơn vẫn phải hiện đủ.
    [Fact]
    public async Task GetMyOrder_ChiNhanhDaXoaMem_VanHienDonAsync()
    {
        var order = Seed(OrderStatus.Redeemed);
        await SaveAsync();
        // Đặt cờ trực tiếp: Remove() trên entity còn đơn đang theo dõi sẽ bị EF chặn trước khi tới bước xoá mềm.
        _store.IsDeleted = true;
        await SaveAsync();
        (await _context.Stores.AnyAsync(s => s.Id == _store.Id)).Should().BeFalse();

        var result = await CreateCustomerService().GetMyOrderAsync(order.Id, CancellationToken.None);

        result.OrderCode.Should().Be(order.OrderCode);
        result.StoreName.Should().Be("Chi nhánh Nguyễn Huệ");
    }

    [Fact]
    public async Task GetMyOrder_DonCuaNguoiKhac_Bao404Async()
    {
        var order = Seed(OrderStatus.Paid, customerId: Guid.NewGuid());
        await SaveAsync();

        var act = () => CreateCustomerService().GetMyOrderAsync(order.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound);
    }

    // FR-S04: mặc định ẩn đơn chưa trả tiền, đơn chờ trao lên đầu theo giờ thanh toán.
    [Fact]
    public async Task GetSaleBatchOrders_MacDinh_AnDonChuaTraVaDonChoTraoLenDauAsync()
    {
        var redeemed = Seed(OrderStatus.Redeemed, paidMinutesAgo: 50);
        var paidLater = Seed(OrderStatus.Paid, paidMinutesAgo: 10);
        var paidEarlier = Seed(OrderStatus.Paid, paidMinutesAgo: 40);
        Seed(OrderStatus.PendingPayment);
        await SaveAsync();

        var result = await CreateStaffService().GetSaleBatchOrdersAsync(_batch.Id, new OrderListQuery(), CancellationToken.None);

        result.Items.Select(o => o.Id).Should().Equal(paidEarlier.Id, paidLater.Id, redeemed.Id);
        result.Items[0].CustomerName.Should().Be("Nguyễn Văn An");
        result.Items[0].Items.Should().ContainSingle().Which.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task GetSaleBatchOrders_LocStatusPending_VanXemDuocAsync()
    {
        var pending = Seed(OrderStatus.PendingPayment);
        Seed(OrderStatus.Paid, paidMinutesAgo: 10);
        await SaveAsync();

        var result = await CreateStaffService().GetSaleBatchOrdersAsync(
            _batch.Id, new OrderListQuery { Status = OrderStatus.PendingPayment }, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
    }

    // BR-10
    [Fact]
    public async Task GetSaleBatchOrders_NhanVienChiNhanhKhac_Bao403Async()
    {
        var act = () => CreateService([AppRoles.StoreStaff], storeId: Guid.NewGuid())
            .GetSaleBatchOrdersAsync(_batch.Id, new OrderListQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope && e.StatusCode == 403);
    }

    [Fact]
    public async Task GetSaleBatchOrders_BrandManagerCungThuongHieu_XemDuocAsync()
    {
        Seed(OrderStatus.Paid, paidMinutesAgo: 10);
        await SaveAsync();

        var result = await CreateService([AppRoles.BrandManager], brandId: _brandId)
            .GetSaleBatchOrdersAsync(_batch.Id, new OrderListQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSaleBatchOrders_DotKhongTonTai_Bao404Async()
    {
        var act = () => CreateStaffService().GetSaleBatchOrdersAsync(Guid.NewGuid(), new OrderListQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound);
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void OrderListQueryValidator_KiemPageVaPageSize(int page, int pageSize, bool valid)
    {
        new OrderListQueryValidator().Validate(new OrderListQuery { Page = page, PageSize = pageSize }).IsValid.Should().Be(valid);
    }

    private Order Seed(OrderStatus status, int createdMinutesAgo = 0, int? paidMinutesAgo = null, Guid? customerId = null)
    {
        var order = new Order
        {
            OrderCode = "FR-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerUserId = customerId ?? _customer.Id,
            StoreId = _store.Id,
            SaleBatchId = _batch.Id,
            Status = status,
            PaidAt = paidMinutesAgo is { } m ? _now.AddMinutes(-m) : null,
            TotalAmount = 35000,
        };
        order.Items.Add(new OrderItem { OfferId = Guid.NewGuid(), Quantity = 1, UnitPrice = 35000, LineTotal = 35000, OfferNameSnapshot = "Túi mù bánh ngọt" });
        _context.Orders.Add(order);
        _pendingCreatedAt[order] = _now.AddMinutes(-createdMinutesAgo);
        return order;
    }

    private readonly Dictionary<Order, DateTime> _pendingCreatedAt = [];

    /// <summary>DbContext tự ghi CreatedAt lúc thêm, nên đặt lại sau khi lưu để có thứ tự thời gian.</summary>
    private async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
        foreach (var (order, createdAt) in _pendingCreatedAt)
        {
            order.CreatedAt = createdAt;
        }

        _pendingCreatedAt.Clear();
        await _context.SaveChangesAsync();
    }

    private OrderService CreateCustomerService()
        => CreateService([AppRoles.Customer], userId: _customer.Id);

    private OrderService CreateStaffService()
        => CreateService([AppRoles.StoreStaff], storeId: _store.Id);

    private OrderService CreateService(string[] roles, Guid? userId = null, Guid? storeId = null, Guid? brandId = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.Setup(u => u.Roles).Returns(roles);
        currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(roles.Contains);
        currentUser.Setup(u => u.StoreId).Returns(storeId);
        currentUser.Setup(u => u.BrandId).Returns(brandId);

        return new OrderService(
            new UnitOfWork(_context),
            new MapperConfiguration(c => c.AddProfile<OrderingProfile>()).CreateMapper(),
            currentUser.Object,
            Mock.Of<IQrTicketClient>(),
            new FixedTimeProvider(_now),
            NullLogger<OrderService>.Instance,
            Options.Create(new OrderingOptions()));
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
