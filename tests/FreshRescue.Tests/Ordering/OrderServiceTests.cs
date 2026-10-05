using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.DataAccess;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.Ordering;

/// <summary>
/// Quy tắc nghiệp vụ của đặt hàng. Tranh chấp đồng thời (NFR-01) cần SQL Server thật vì
/// InMemory không có rowversion — không kiểm ở đây.
/// </summary>
public class OrderServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 5, 13, 0, 0, DateTimeKind.Utc);

    private readonly Guid _customerId = Guid.NewGuid();
    private readonly FreshRescueDbContext _context;
    private readonly SaleBatch _batch;
    private readonly Offer _bag;
    private readonly Offer _bread;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // InMemory không có giao dịch; service vẫn mở giao dịch như với SQL Server.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));

        var store = new Store { Name = "Chi nhánh Nguyễn Huệ", AddressLine = "12 Nguyễn Huệ", Ward = "Bến Nghé", District = "Quận 1", City = "TP.HCM" };
        _batch = NewBatch(store, SaleBatchStatus.Active);
        _bag = NewOffer(_batch, "Túi mù bánh ngọt", price: 35000, remaining: 5);
        _bread = NewOffer(_batch, "Bánh mì que", price: 35005, remaining: 5);

        _context.AddRange(store, _batch, _bag, _bread);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_HopLe_TruTonKhoVaTraDonChoThanhToanAsync()
    {
        var result = await CreateAsync(new CreateOrderItemRequest(_bag.Id, 2));

        result.Status.Should().Be(OrderStatus.PendingPayment);
        result.OrderCode.Should().MatchRegex("^FR-[A-HJ-NP-Z2-9]{8}$");
        result.TotalAmount.Should().Be(70000);
        result.StoreAddress.Should().Be("12 Nguyễn Huệ, Bến Nghé, Quận 1");
        result.PaymentDeadline.Should().Be(_now.AddMinutes(10));
        result.Items.Should().ContainSingle().Which.OfferName.Should().Be("Túi mù bánh ngọt");
        (await ReloadOfferAsync(_bag.Id)).RemainingQuantity.Should().Be(3);
    }

    // BR-20: phí làm tròn trước, phần cửa hàng là phần còn lại.
    [Fact]
    public async Task CreateAsync_PhiLeNuaDong_LamTronLenVaTongVanKhopAsync()
    {
        var result = await CreateAsync(new CreateOrderItemRequest(_bread.Id, 1));

        var order = await _context.Orders.AsNoTracking().SingleAsync(o => o.Id == result.Id);
        order.TotalAmount.Should().Be(35005);
        order.PlatformFee.Should().Be(3501);
        order.StorePayoutAmount.Should().Be(31504);
    }

    // BR-05: hạn thanh toán không vượt quá PickupEndTime.
    [Fact]
    public async Task CreateAsync_SatGioDongNhanHang_HanThanhToanBangPickupEndTimeAsync()
    {
        _batch.PickupEndTime = _now.AddMinutes(4);
        await _context.SaveChangesAsync();

        var result = await CreateAsync(new CreateOrderItemRequest(_bag.Id, 1));

        result.PaymentDeadline.Should().Be(_now.AddMinutes(4));
    }

    [Fact]
    public async Task CreateAsync_DotBanChuaKichHoat_BaoBatchNotActiveAsync()
    {
        _batch.Status = SaleBatchStatus.Scheduled;
        await _context.SaveChangesAsync();

        await ShouldFailAsync(ErrorCodes.BatchNotActive, new CreateOrderItemRequest(_bag.Id, 1));
    }

    [Fact]
    public async Task CreateAsync_DaQuaGioBan_BaoSaleWindowClosedAsync()
    {
        _batch.SaleEndTime = _now.AddMinutes(-1);
        await _context.SaveChangesAsync();

        await ShouldFailAsync(ErrorCodes.SaleWindowClosed, new CreateOrderItemRequest(_bag.Id, 1));
    }

    [Fact]
    public async Task CreateAsync_MonThuocDotKhac_BaoOfferCrossBatchAsync()
    {
        var otherBatch = NewBatch(_batch.Store, SaleBatchStatus.Active);
        var otherOffer = NewOffer(otherBatch, "Cơm hộp", price: 30000, remaining: 5);
        _context.AddRange(otherBatch, otherOffer);
        await _context.SaveChangesAsync();

        await ShouldFailAsync(
            ErrorCodes.OfferCrossBatch,
            new CreateOrderItemRequest(_bag.Id, 1),
            new CreateOrderItemRequest(otherOffer.Id, 1));
    }

    [Fact]
    public async Task CreateAsync_KhongDuTonKho_BaoOfferSoldOutVaKhongTruKhoAsync()
    {
        _bag.RemainingQuantity = 1;
        await _context.SaveChangesAsync();

        await ShouldFailAsync(ErrorCodes.OfferSoldOut, new CreateOrderItemRequest(_bag.Id, 2));
        (await ReloadOfferAsync(_bag.Id)).RemainingQuantity.Should().Be(1);
    }

    // BR-06: đơn đã nhận hàng (Redeemed) vẫn tính vào giới hạn 3 suất.
    [Fact]
    public async Task CreateAsync_CongDonCaDonDaNhan_BaoPurchaseLimitExceededAsync()
    {
        await SeedExistingOrderAsync(OrderStatus.Redeemed, quantity: 2);

        await ShouldFailAsync(ErrorCodes.PurchaseLimitExceeded, new CreateOrderItemRequest(_bag.Id, 2));
    }

    [Fact]
    public async Task CreateAsync_DonCuDaHuy_KhongTinhVaoGioiHanAsync()
    {
        await SeedExistingOrderAsync(OrderStatus.Cancelled, quantity: 3);

        var result = await CreateAsync(new CreateOrderItemRequest(_bag.Id, 3));

        result.Items.Single().Quantity.Should().Be(3);
    }

    private Task<OrderDto> CreateAsync(params CreateOrderItemRequest[] items)
        => CreateService().CreateAsync(new CreateOrderRequest(_batch.Id, [.. items]), CancellationToken.None);

    private async Task ShouldFailAsync(string errorCode, params CreateOrderItemRequest[] items)
    {
        var act = () => CreateAsync(items);
        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == errorCode);
    }

    private OrderService CreateService()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_customerId);
        var mapper = new MapperConfiguration(c => c.AddProfile<OrderingProfile>()).CreateMapper();

        return new OrderService(
            new FreshRescue.DataAccess.UnitOfWork.UnitOfWork(_context),
            mapper,
            currentUser.Object,
            new FixedTimeProvider(_now),
            NullLogger<OrderService>.Instance);
    }

    private async Task SeedExistingOrderAsync(OrderStatus status, int quantity)
    {
        var order = new Order
        {
            OrderCode = "FR-EXISTING",
            CustomerUserId = _customerId,
            StoreId = _batch.StoreId,
            SaleBatchId = _batch.Id,
            Status = status,
        };
        order.Items.Add(new OrderItem { OfferId = _bag.Id, Quantity = quantity, OfferNameSnapshot = _bag.Name });
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }

    private Task<Offer> ReloadOfferAsync(Guid id)
        => _context.Offers.AsNoTracking().SingleAsync(o => o.Id == id);

    private static SaleBatch NewBatch(Store store, SaleBatchStatus status) => new()
    {
        Store = store,
        StoreId = store.Id,
        Status = status,
        SaleStartTime = _now.AddHours(-1),
        SaleEndTime = _now.AddHours(1),
        PickupStartTime = _now.AddMinutes(-30),
        PickupEndTime = _now.AddHours(2),
    };

    private static Offer NewOffer(SaleBatch batch, string name, decimal price, int remaining) => new()
    {
        SaleBatchId = batch.Id,
        Name = name,
        ActualPrice = price,
        OriginalPrice = price * 3,
        TotalQuantity = remaining,
        RemainingQuantity = remaining,
    };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
