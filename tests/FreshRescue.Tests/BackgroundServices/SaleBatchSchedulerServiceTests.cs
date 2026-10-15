using FluentAssertions;
using FreshRescue.API.BackgroundServices;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Sales;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.BackgroundServices;

public class SaleBatchSchedulerServiceTests
{
    private static readonly DateTime _baseTime = new(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly Store _store;

    public SaleBatchSchedulerServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new FreshRescueDbContext(options, new MutableTimeProvider(_baseTime));

        _store = new Store
        {
            BrandId = Guid.NewGuid(),
            Name = "Chi nhánh Vincom Đồng Khởi",
            AddressLine = "72 Lê Thánh Tôn",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM"
        };

        _context.Stores.Add(_store);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ActivateDueBatches_ToiSaleStartTime_ChuyenScheduledSangActiveAsync()
    {
        // Arrange: batch Scheduled với SaleStartTime là 10:00 (bằng hiện tại)
        var batch = new SaleBatch
        {
            StoreId = _store.Id,
            Name = "Đợt sáng",
            Status = SaleBatchStatus.Scheduled,
            SaleStartTime = _baseTime,
            SaleEndTime = _baseTime.AddHours(2),
            PickupStartTime = _baseTime.AddHours(2),
            PickupEndTime = _baseTime.AddHours(4),
        };
        _context.SaleBatches.Add(batch);
        await _context.SaveChangesAsync();

        var service = CreateSaleBatchService(_baseTime);

        // Act
        var count = await service.ActivateDueBatchesAsync(CancellationToken.None);

        // Assert
        count.Should().Be(1);
        var updated = await _context.SaleBatches.FindAsync(batch.Id);
        updated!.Status.Should().Be(SaleBatchStatus.Active);
        updated.ActivatedAt.Should().Be(_baseTime);
    }

    [Fact]
    public async Task ActivateDueBatches_ChuaToiSaleStartTime_GiuNguyenScheduledAsync()
    {
        // Arrange: batch Scheduled bắt đầu lúc 11:00 (tương lai)
        var batch = new SaleBatch
        {
            StoreId = _store.Id,
            Name = "Đợt trưa",
            Status = SaleBatchStatus.Scheduled,
            SaleStartTime = _baseTime.AddHours(1),
            SaleEndTime = _baseTime.AddHours(3),
            PickupStartTime = _baseTime.AddHours(3),
            PickupEndTime = _baseTime.AddHours(5),
        };
        _context.SaleBatches.Add(batch);
        await _context.SaveChangesAsync();

        var service = CreateSaleBatchService(_baseTime);

        // Act
        var count = await service.ActivateDueBatchesAsync(CancellationToken.None);

        // Assert
        count.Should().Be(0);
        var updated = await _context.SaleBatches.FindAsync(batch.Id);
        updated!.Status.Should().Be(SaleBatchStatus.Scheduled);
        updated.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ActivateDueBatches_DotDraft_KhongBiChuyenAsync()
    {
        // Arrange: đợt Draft dù đã qua thời gian vẫn không tự kích hoạt
        var batch = new SaleBatch
        {
            StoreId = _store.Id,
            Name = "Đợt nháp",
            Status = SaleBatchStatus.Draft,
            SaleStartTime = _baseTime.AddHours(-1),
            SaleEndTime = _baseTime.AddHours(1),
            PickupStartTime = _baseTime.AddHours(1),
            PickupEndTime = _baseTime.AddHours(3),
        };
        _context.SaleBatches.Add(batch);
        await _context.SaveChangesAsync();

        var service = CreateSaleBatchService(_baseTime);

        // Act
        var count = await service.ActivateDueBatchesAsync(CancellationToken.None);

        // Assert
        count.Should().Be(0);
        var updated = await _context.SaleBatches.FindAsync(batch.Id);
        updated!.Status.Should().Be(SaleBatchStatus.Draft);
    }

    [Fact]
    public async Task CloseDueBatches_ToiSaleEndTime_KhongDoiTrangThai_VanGiuActiveAsync()
    {
        // Yêu cầu bắt buộc 01-REQUIREMENTS.md §6.5:
        // Không có sự kiện đổi trạng thái tại SaleEndTime! Khách vẫn tới nhận hàng trong [SaleEndTime, PickupEndTime].
        var saleEndTime = _baseTime.AddHours(2);
        var pickupEndTime = _baseTime.AddHours(4);

        var batch = new SaleBatch
        {
            StoreId = _store.Id,
            Name = "Đợt trưa",
            Status = SaleBatchStatus.Active,
            SaleStartTime = _baseTime,
            SaleEndTime = saleEndTime,
            PickupStartTime = saleEndTime,
            PickupEndTime = pickupEndTime,
        };
        _context.SaleBatches.Add(batch);
        await _context.SaveChangesAsync();

        // Thời điểm hiện tại là đúng lúc hết giờ bán (SaleEndTime)
        var serviceAtSaleEnd = CreateSaleBatchService(saleEndTime);

        // Act
        var count = await serviceAtSaleEnd.CloseDueBatchesAsync(CancellationToken.None);

        // Assert
        count.Should().Be(0, "Không được đóng đợt tại SaleEndTime");
        var updated = await _context.SaleBatches.FindAsync(batch.Id);
        updated!.Status.Should().Be(SaleBatchStatus.Active, "Đợt bán phải giữ trạng thái Active trong suốt khung nhận hàng");
        updated.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task CloseDueBatches_ToiPickupEndTime_DongDotVaChotUnsoldQuantityAsync()
    {
        // Arrange: đợt Active tới PickupEndTime kèm các Offer còn dư
        var pickupEndTime = _baseTime.AddHours(4);

        var batch = new SaleBatch
        {
            StoreId = _store.Id,
            Name = "Đợt chiều",
            Status = SaleBatchStatus.Active,
            SaleStartTime = _baseTime,
            SaleEndTime = _baseTime.AddHours(2),
            PickupStartTime = _baseTime.AddHours(2),
            PickupEndTime = pickupEndTime,
        };
        _context.SaleBatches.Add(batch);
        await _context.SaveChangesAsync();

        var offer1 = new Offer
        {
            SaleBatchId = batch.Id,
            Name = "Bánh sừng bò",
            ActualPrice = 20000,
            OriginalPrice = 60000,
            TotalQuantity = 10,
            RemainingQuantity = 3 // Còn dư 3
        };

        var offer2 = new Offer
        {
            SaleBatchId = batch.Id,
            Name = "Bánh tart trứng",
            ActualPrice = 15000,
            OriginalPrice = 45000,
            TotalQuantity = 8,
            RemainingQuantity = 2 // Còn dư 2
        };

        _context.Offers.AddRange(offer1, offer2);
        await _context.SaveChangesAsync();

        // Hiện tại là đúng hoặc sau PickupEndTime
        var currentTime = pickupEndTime.AddMinutes(1);
        var service = CreateSaleBatchService(currentTime);

        // Act
        var count = await service.CloseDueBatchesAsync(CancellationToken.None);

        // Assert
        count.Should().Be(1);

        var updatedBatch = await _context.SaleBatches.FindAsync(batch.Id);
        updatedBatch!.Status.Should().Be(SaleBatchStatus.Closed);
        updatedBatch.ClosedAt.Should().Be(currentTime);
        updatedBatch.TotalUnsoldQuantity.Should().Be(5, "Tổng số lượng chưa bán = 3 + 2 = 5 (OQ-15)");

        var updatedOffer1 = await _context.Offers.FindAsync(offer1.Id);
        updatedOffer1!.UnsoldQuantity.Should().Be(3);

        var updatedOffer2 = await _context.Offers.FindAsync(offer2.Id);
        updatedOffer2!.UnsoldQuantity.Should().Be(2);
    }

    [Fact]
    public async Task SaleBatchSchedulerService_HostedService_ProcessBatchesAsync_ChayThanhCongAsync()
    {
        // Arrange
        var batch = new SaleBatch
        {
            StoreId = _store.Id,
            Name = "Đợt test scheduler",
            Status = SaleBatchStatus.Scheduled,
            SaleStartTime = _baseTime.AddMinutes(-1),
            SaleEndTime = _baseTime.AddHours(1),
            PickupStartTime = _baseTime.AddHours(1),
            PickupEndTime = _baseTime.AddHours(2),
        };
        _context.SaleBatches.Add(batch);
        await _context.SaveChangesAsync();

        var clock = new MutableTimeProvider(_baseTime);
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var saleBatchService = CreateSaleBatchService(_baseTime);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(ISaleBatchService))).Returns(saleBatchService);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var scheduler = new SaleBatchSchedulerService(
            scopeFactoryMock.Object,
            clock,
            NullLogger<SaleBatchSchedulerService>.Instance);

        // Act
        await scheduler.ProcessBatchesAsync(CancellationToken.None);

        // Assert
        var updated = await _context.SaleBatches.FindAsync(batch.Id);
        updated!.Status.Should().Be(SaleBatchStatus.Active);
    }

    private SaleBatchService CreateSaleBatchService(DateTime now)
    {
        var uow = new UnitOfWork(_context);
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(o => o.ProcessNoShowsAsync(It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync(0);

        return new SaleBatchService(
            uow,
            orderServiceMock.Object,
            Mock.Of<AutoMapper.IMapper>(),
            Mock.Of<FreshRescue.Business.Common.ICurrentUser>(),
            new MutableTimeProvider(now),
            NullLogger<SaleBatchService>.Instance);
    }

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }
}
