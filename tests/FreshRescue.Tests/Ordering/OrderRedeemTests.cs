using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Business.Features.Ordering.Validators;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.Repositories;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FreshRescue.Tests.Ordering;

/// <summary>
/// Đối soát QR (FR-S05, BR-08, BR-09, BR-10). Câu UPDATE có điều kiện chạy bằng ExecuteUpdate mà
/// InMemory không hỗ trợ, nên repository đơn được thay bằng bản mô phỏng cùng điều kiện WHERE.
/// </summary>
public class OrderRedeemTests
{
    private const string ValidToken = "valid-token";

    private static readonly DateTime _now = new(2026, 10, 14, 13, 0, 0, DateTimeKind.Utc);

    private readonly Guid _staffUserId = Guid.NewGuid();
    private readonly Guid _brandId = Guid.NewGuid();
    private readonly FreshRescueDbContext _context;
    private readonly Store _store;
    private readonly Order _order;
    private readonly Mock<IQrTicketClient> _qrTicketClient = new();
    private readonly FakeOrderRepository _orders;

    public OrderRedeemTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));
        _orders = new FakeOrderRepository(_context);

        _store = new Store { BrandId = _brandId, Name = "Chi nhánh Nguyễn Huệ", AddressLine = "12 Nguyễn Huệ", Ward = "Bến Nghé", District = "Quận 1", City = "TP.HCM" };
        var batch = new SaleBatch
        {
            Store = _store,
            StoreId = _store.Id,
            Status = SaleBatchStatus.Active,
            SaleStartTime = _now.AddHours(-1),
            SaleEndTime = _now.AddHours(1),
            PickupStartTime = _now.AddMinutes(-30),
            PickupEndTime = _now.AddHours(2),
        };
        var customer = new AppUser { FullName = "Nguyễn Văn An", UserName = "an@example.com", Email = "an@example.com" };
        _order = new Order
        {
            OrderCode = "FR-8A3K9MQ2",
            CustomerUserId = customer.Id,
            StoreId = _store.Id,
            SaleBatchId = batch.Id,
            Status = OrderStatus.Paid,
            PaidAt = _now.AddMinutes(-40),
        };
        _order.Items.Add(new OrderItem { OfferId = Guid.NewGuid(), Quantity = 2, OfferNameSnapshot = "Túi mù bánh ngọt" });
        var ticket = new QrTicket { OrderId = _order.Id, Token = ValidToken, IssuedAt = _now.AddMinutes(-40), ExpiresAt = batch.PickupEndTime };

        _context.AddRange(_store, batch, customer, _order, ticket);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        _qrTicketClient
            .Setup(c => c.VerifyAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QrTicketVerification(true, _order.Id, _store.Id, null));
    }

    [Fact]
    public async Task RedeemAsync_QuetQrDungChiNhanh_TraoHangVaGhiVetAsync()
    {
        var result = await CreateStaffService().RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        result.OrderId.Should().Be(_order.Id);
        result.OrderCode.Should().Be("FR-8A3K9MQ2");
        result.CustomerName.Should().Be("Nguyễn Văn An");
        result.RedeemedAt.Should().Be(_now);
        result.Items.Should().ContainSingle().Which.Should().Be(new RedemptionItemDto { OfferName = "Túi mù bánh ngọt", Quantity = 2 });

        var order = await _context.Orders.AsNoTracking().SingleAsync(o => o.Id == _order.Id);
        order.Status.Should().Be(OrderStatus.Redeemed);
        order.RedeemedAt.Should().Be(_now);

        // NFR-05: ai quét, lúc nào.
        var ticket = await _context.QrTickets.AsNoTracking().SingleAsync(t => t.OrderId == _order.Id);
        ticket.Status.Should().Be(QrTicketStatus.Used);
        ticket.UsedAt.Should().Be(_now);
        ticket.UsedByStaffUserId.Should().Be(_staffUserId);
    }

    // FR-S06: nhân viên gõ tay, có thể gõ chữ thường hoặc dư khoảng trắng.
    [Fact]
    public async Task RedeemAsync_NhapMaDonTay_TraoHangAsync()
    {
        var result = await CreateStaffService().RedeemAsync(new RedeemOrderRequest(null, "  fr-8a3k9mq2 "), CancellationToken.None);

        result.OrderId.Should().Be(_order.Id);
        _qrTicketClient.Verify(c => c.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // BR-09
    [Fact]
    public async Task RedeemAsync_QuetLanHai_Bao409VaKhongDoiGioTraoAsync()
    {
        await CreateStaffService().RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        var act = () => CreateStaffService(now: _now.AddMinutes(1))
            .RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.OrderAlreadyRedeemed && e.StatusCode == 409);
        (await _context.Orders.AsNoTracking().SingleAsync(o => o.Id == _order.Id)).RedeemedAt.Should().Be(_now);
    }

    // BR-09: hai quầy quét cùng lúc — cả hai đều đọc thấy Paid, chỉ WHERE Status = Paid chặn được người thứ hai.
    [Fact]
    public async Task RedeemAsync_QuayKhacVuaQuetXongGiuaLucDocVaGhi_Bao409Async()
    {
        _orders.BeforeUpdate = async () =>
        {
            var order = await _context.Orders.SingleAsync(o => o.Id == _order.Id);
            order.Status = OrderStatus.Redeemed;
            await _context.SaveChangesAsync();
        };

        var act = () => CreateStaffService().RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.OrderAlreadyRedeemed);
        (await _context.QrTickets.AsNoTracking().SingleAsync()).Status.Should().Be(QrTicketStatus.Active);
    }

    [Theory]
    [InlineData("QR_INVALID_SIGNATURE")]
    [InlineData("QR_EXPIRED")]
    public async Task RedeemAsync_QrServiceTuChoi_TraDungMaLoiAsync(string errorCode)
    {
        _qrTicketClient
            .Setup(c => c.VerifyAsync("bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QrTicketVerification(false, null, null, errorCode));

        var act = () => CreateStaffService().RedeemAsync(new RedeemOrderRequest("bad-token", null), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == errorCode && e.StatusCode == 400);
        await ShouldStillBePaidAsync();
    }

    // BR-10
    [Fact]
    public async Task RedeemAsync_NhanVienChiNhanhKhac_Bao403KemTenChiNhanhDungAsync()
    {
        var act = () => CreateService([AppRoles.StoreStaff], storeId: Guid.NewGuid())
            .RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope && e.StatusCode == 403))
            .Which.Message.Should().Contain("Chi nhánh Nguyễn Huệ");
        await ShouldStillBePaidAsync();
    }

    // BR-10: chủ chuỗi đứng quầy ở bất kỳ chi nhánh nào của thương hiệu mình.
    [Fact]
    public async Task RedeemAsync_BrandManagerCungThuongHieu_TraoHangAsync()
    {
        var result = await CreateService([AppRoles.BrandManager], brandId: _brandId)
            .RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        result.OrderId.Should().Be(_order.Id);
    }

    [Fact]
    public async Task RedeemAsync_BrandManagerThuongHieuKhac_Bao403Async()
    {
        var act = () => CreateService([AppRoles.BrandManager], brandId: Guid.NewGuid())
            .RedeemAsync(new RedeemOrderRequest(ValidToken, null), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope);
    }

    // BR-08: [PickupStartTime − 5', PickupEndTime], tính cả hai đầu mút.
    [Theory]
    [InlineData(-35, true)]
    [InlineData(-36, false)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public async Task RedeemAsync_TheoKhungGioNhan_ChiTraoTrongCuaSoAsync(int minutesFromNow, bool allowed)
    {
        var act = () => CreateStaffService(now: _now.AddMinutes(minutesFromNow))
            .RedeemAsync(new RedeemOrderRequest(null, "FR-8A3K9MQ2"), CancellationToken.None);

        if (allowed)
        {
            await act.Should().NotThrowAsync();
        }
        else
        {
            await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.RedeemWindowClosed && e.StatusCode == 400);
            await ShouldStillBePaidAsync();
        }
    }

    // Contract chưa có mã riêng — chốt 14/10 dùng NOT_FOUND.
    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.NoShow)]
    public async Task RedeemAsync_DonKhongODangPaid_Bao404Async(OrderStatus status)
    {
        var order = await _context.Orders.SingleAsync(o => o.Id == _order.Id);
        order.Status = status;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var act = () => CreateStaffService().RedeemAsync(new RedeemOrderRequest(null, "FR-8A3K9MQ2"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound && e.StatusCode == 404);
    }

    [Fact]
    public async Task RedeemAsync_MaDonKhongTonTai_Bao404Async()
    {
        var act = () => CreateStaffService().RedeemAsync(new RedeemOrderRequest(null, "FR-ZZZZZZZZ"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound);
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("  ", "", false)]
    [InlineData("token", "FR-8A3K9MQ2", false)]
    [InlineData("token", null, true)]
    [InlineData(null, "FR-8A3K9MQ2", true)]
    public void Validator_GuiDungMotTrongHai(string? token, string? orderCode, bool valid)
    {
        new RedeemOrderRequestValidator().Validate(new RedeemOrderRequest(token, orderCode)).IsValid.Should().Be(valid);
    }

    private async Task ShouldStillBePaidAsync()
        => (await _context.Orders.AsNoTracking().SingleAsync(o => o.Id == _order.Id)).Status.Should().Be(OrderStatus.Paid);

    private OrderService CreateStaffService(DateTime? now = null)
        => CreateService([AppRoles.StoreStaff], storeId: _store.Id, now: now);

    private OrderService CreateService(string[] roles, Guid? storeId = null, Guid? brandId = null, DateTime? now = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_staffUserId);
        currentUser.Setup(u => u.Roles).Returns(roles);
        currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(roles.Contains);
        currentUser.Setup(u => u.StoreId).Returns(storeId);
        currentUser.Setup(u => u.BrandId).Returns(brandId);

        var real = new UnitOfWork(_context);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Orders).Returns(_orders);
        uow.Setup(u => u.QrTickets).Returns(real.QrTickets);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(real.SaveChangesAsync);
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(real.BeginTransactionAsync);

        return new OrderService(
            uow.Object,
            new MapperConfiguration(c => c.AddProfile<OrderingProfile>()).CreateMapper(),
            currentUser.Object,
            _qrTicketClient.Object,
            new FixedTimeProvider(now ?? _now),
            NullLogger<OrderService>.Instance,
            Options.Create(new OrderingOptions()));
    }

    /// <summary>Cùng điều kiện WHERE Id = @id AND Status = Paid với OrderRepository thật.</summary>
    private sealed class FakeOrderRepository(FreshRescueDbContext context)
        : GenericRepository<Order>(context), IOrderRepository
    {
        /// <summary>Chen một thay đổi vào giữa lúc service đọc và lúc ghi.</summary>
        public Func<Task>? BeforeUpdate { get; set; }

        public async Task<bool> TryMarkRedeemedAsync(Guid orderId, DateTime redeemedAt, CancellationToken cancellationToken)
        {
            if (BeforeUpdate is not null)
            {
                await BeforeUpdate();
            }

            var order = await Set.SingleOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Paid, cancellationToken);
            if (order is null)
            {
                return false;
            }

            order.Status = OrderStatus.Redeemed;
            order.RedeemedAt = redeemedAt;
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
