using FluentAssertions;
using FreshRescue.API.Controllers;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.Ordering;

public class BrandStatsTests
{
    private readonly FreshRescueDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly Guid _brandId = Guid.NewGuid();
    private readonly Store _storeActiveWithOrders;
    private readonly Store _storeActiveNoOrders;
    private readonly Store _storeDeletedWithOrders;
    private readonly Store _storeDeletedNoOrders;

    public BrandStatsTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FreshRescueDbContext(options, TimeProvider.System);
        _uow = new UnitOfWork(_context);

        _storeActiveWithOrders = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _brandId,
            Name = "Chi nhánh Nguyễn Huệ",
            AddressLine = "12 Nguyễn Huệ",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM",
            RatingAverage = 4.8m,
            IsDeleted = false
        };

        _storeActiveNoOrders = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _brandId,
            Name = "Chi nhánh Lê Lợi",
            AddressLine = "50 Lê Lợi",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM",
            RatingAverage = 4.2m,
            IsDeleted = false
        };

        _storeDeletedWithOrders = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _brandId,
            Name = "Chi nhánh Đã Đóng Cửa (Có đơn)",
            AddressLine = "99 Hai Bà Trưng",
            Ward = "Đa Kao",
            District = "Quận 1",
            City = "TP.HCM",
            RatingAverage = 4.0m,
            IsDeleted = true
        };

        _storeDeletedNoOrders = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _brandId,
            Name = "Chi nhánh Đã Đóng Cửa (Không đơn)",
            AddressLine = "100 Hai Bà Trưng",
            Ward = "Đa Kao",
            District = "Quận 1",
            City = "TP.HCM",
            RatingAverage = 3.5m,
            IsDeleted = true
        };

        _context.Stores.AddRange(_storeActiveWithOrders, _storeActiveNoOrders, _storeDeletedWithOrders, _storeDeletedNoOrders);
        _context.SaveChanges();
    }

    [Fact]
    public void BrandStatsCalculator_TinhToanChuanBR25Async()
    {
        // Arrange
        var orders = new List<Order>
        {
            // Đơn 1: Redeemed -> tính doanh thu + tính rescued
            new()
            {
                StoreId = _storeActiveWithOrders.Id,
                Status = OrderStatus.Redeemed,
                RefundStatus = RefundStatus.None,
                StorePayoutAmount = 100_000,
                Items = [new OrderItem { Quantity = 2 }]
            },
            // Đơn 2: NoShow -> tính doanh thu, KHÔNG tính rescued (BR-25, BR-19)
            new()
            {
                StoreId = _storeActiveWithOrders.Id,
                Status = OrderStatus.NoShow,
                RefundStatus = RefundStatus.None,
                StorePayoutAmount = 50_000,
                Items = [new OrderItem { Quantity = 1 }]
            },
            // Đơn 3: Cancelled -> KHÔNG tính doanh thu, KHÔNG tính rescued
            new()
            {
                StoreId = _storeActiveWithOrders.Id,
                Status = OrderStatus.Cancelled,
                RefundStatus = RefundStatus.Refunded,
                StorePayoutAmount = 70_000,
                Items = [new OrderItem { Quantity = 1 }]
            }
        };

        var stores = new List<Store> { _storeActiveWithOrders, _storeActiveNoOrders };

        // Act
        var result = BrandStatsCalculator.Calculate(orders, stores);

        // Assert
        result.TotalRevenue.Should().Be(150_000); // 100_000 + 50_000
        result.TotalRescuedPortions.Should().Be(2); // Chỉ đơn Redeemed
        result.ByStore.Should().HaveCount(2);

        var store1Stat = result.ByStore.Single(s => s.StoreId == _storeActiveWithOrders.Id);
        store1Stat.Revenue.Should().Be(150_000);
        store1Stat.RescuedPortions.Should().Be(2);
        store1Stat.NoShowRate.Should().Be(0.5); // 1 NoShow / (1 Redeemed + 1 NoShow) = 0.5
        store1Stat.RatingAverage.Should().Be(4.8m);

        var store2Stat = result.ByStore.Single(s => s.StoreId == _storeActiveNoOrders.Id);
        store2Stat.Revenue.Should().Be(0);
        store2Stat.RescuedPortions.Should().Be(0);
        store2Stat.NoShowRate.Should().Be(0.0); // Mẫu 0 -> trả 0
        store2Stat.RatingAverage.Should().Be(4.2m);
    }

    [Fact]
    public async Task GetBrandStatsAsync_LocThanhCongKemChiNhanhXoaMemCoDonAsync()
    {
        // Arrange
        var baseDate = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

        var order1 = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = _storeActiveWithOrders.Id,
            CreatedAt = baseDate,
            Status = OrderStatus.Redeemed,
            RefundStatus = RefundStatus.None,
            StorePayoutAmount = 80_000,
            Items = [new OrderItem { Quantity = 3 }]
        };

        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = _storeDeletedWithOrders.Id, // Thuộc chi nhánh đã xoá mềm
            CreatedAt = baseDate.AddDays(1),
            Status = OrderStatus.Redeemed,
            RefundStatus = RefundStatus.None,
            StorePayoutAmount = 60_000,
            Items = [new OrderItem { Quantity = 2 }]
        };

        var orderOutside = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = _storeActiveWithOrders.Id,
            CreatedAt = baseDate.AddDays(-10), // Ngoài khoảng lọc
            Status = OrderStatus.Redeemed,
            RefundStatus = RefundStatus.None,
            StorePayoutAmount = 100_000,
            Items = [new OrderItem { Quantity = 5 }]
        };

        _context.Orders.AddRange(order1, order2, orderOutside);
        await _context.SaveChangesAsync();
        order1.CreatedAt = baseDate;
        order2.CreatedAt = baseDate.AddDays(1);
        orderOutside.CreatedAt = baseDate.AddDays(-10);
        await _context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.BrandManager)).Returns(true);
        currentUser.Setup(u => u.BrandId).Returns(_brandId);

        var service = new BrandStatsService(_uow, currentUser.Object, NullLogger<BrandStatsService>.Instance);

        // Act: Lọc từ 2026-10-15 đến 2026-10-17
        var stats = await service.GetBrandStatsAsync(
            from: new DateTime(2026, 10, 15),
            to: new DateTime(2026, 10, 17),
            CancellationToken.None);

        // Assert
        stats.TotalRevenue.Should().Be(140_000); // 80_000 + 60_000
        stats.TotalRescuedPortions.Should().Be(5); // 3 + 2

        // byStore gồm:
        // 1. _storeActiveWithOrders (active, có đơn)
        // 2. _storeActiveNoOrders (active, chưa có đơn -> 0)
        // 3. _storeDeletedWithOrders (xoá mềm nhưng có đơn trong khoảng)
        // KHÔNG gồm: _storeDeletedNoOrders (xoá mềm và không có đơn)
        stats.ByStore.Should().HaveCount(3);
        stats.ByStore.Select(s => s.StoreId).Should().Contain([_storeActiveWithOrders.Id, _storeActiveNoOrders.Id, _storeDeletedWithOrders.Id]);
        stats.ByStore.Select(s => s.StoreId).Should().NotContain(_storeDeletedNoOrders.Id);
    }

    [Fact]
    public async Task GetBrandStatsAsync_FromLonHonTo_NemValidationFailedAsync()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.BrandManager)).Returns(true);
        currentUser.Setup(u => u.BrandId).Returns(_brandId);

        var service = new BrandStatsService(_uow, currentUser.Object, NullLogger<BrandStatsService>.Instance);

        // Act
        var act = () => service.GetBrandStatsAsync(
            from: new DateTime(2026, 10, 20),
            to: new DateTime(2026, 10, 10),
            CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task GetBrandStatsAsync_KhongPhaiBrandManager_NemForbiddenScopeAsync()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.BrandManager)).Returns(false);

        var service = new BrandStatsService(_uow, currentUser.Object, NullLogger<BrandStatsService>.Instance);

        // Act
        var act = () => service.GetBrandStatsAsync(null, null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task BrandStatsController_TraVe200OkAsync()
    {
        // Arrange
        var mockService = new Mock<IBrandStatsService>();
        var expectedDto = new BrandStatsDto
        {
            TotalRevenue = 100_000,
            TotalRescuedPortions = 3,
            ByStore = []
        };

        mockService
            .Setup(s => s.GetBrandStatsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var controller = new BrandStatsController(mockService.Object);

        // Act
        var result = await controller.GetStatsAsync(null, null, CancellationToken.None);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedDto);
    }
}
