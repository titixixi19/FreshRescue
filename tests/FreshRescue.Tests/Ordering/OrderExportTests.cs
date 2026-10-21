using ClosedXML.Excel;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.Ordering;

public class OrderExportTests
{
    private static readonly DateTime _now = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly Guid _brand1Id = Guid.NewGuid();
    private readonly Guid _brand2Id = Guid.NewGuid();
    private readonly Store _store1;
    private readonly Store _storeDeleted;
    private readonly Store _storeBrand2;

    public OrderExportTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FreshRescueDbContext(options, TimeProvider.System);
        _uow = new UnitOfWork(_context);

        _store1 = new Store
        {
            BrandId = _brand1Id,
            Name = "Chi nhánh Quận 1",
            AddressLine = "123 Lê Lợi",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM",
            RatingAverage = 4.5m
        };

        _storeDeleted = new Store
        {
            BrandId = _brand1Id,
            Name = "Chi nhánh Đã Xoá Mềm",
            AddressLine = "456 Nguyễn Huệ",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM",
            IsDeleted = true,
            RatingAverage = 4.0m
        };

        _storeBrand2 = new Store
        {
            BrandId = _brand2Id,
            Name = "Chi nhánh Thương Hiệu Khác",
            AddressLine = "789 Cách Mạng Tháng 8",
            Ward = "Phường 7",
            District = "Tân Bình",
            City = "TP.HCM",
            RatingAverage = 4.8m
        };

        _context.Stores.AddRange(_store1, _storeDeleted, _storeBrand2);

        // Đơn 1: Redeemed (doanh thu 50.000, 2 suất cứu)
        var order1 = CreateOrder(_store1.Id, OrderStatus.Redeemed, 60000, 6000, 54000, 2, _now.AddDays(-2));
        // Đơn 2: NoShow (doanh thu 36.000, không hoàn tiền BR-19)
        var order2 = CreateOrder(_store1.Id, OrderStatus.NoShow, 40000, 4000, 36000, 1, _now.AddDays(-1));
        // Đơn 3: Redeemed ở chi nhánh đã xoá mềm (doanh thu 90.000, 3 suất cứu)
        var order3 = CreateOrder(_storeDeleted.Id, OrderStatus.Redeemed, 100000, 10000, 90000, 3, _now.AddDays(-1));
        // Đơn 4: Cancelled (không tính vào doanh thu BR-25)
        var order4 = CreateOrder(_store1.Id, OrderStatus.Cancelled, 30000, 3000, 27000, 1, _now.AddDays(-3));
        // Đơn 5: Thuộc thương hiệu khác (Brand 2)
        var order5 = CreateOrder(_storeBrand2.Id, OrderStatus.Redeemed, 200000, 20000, 180000, 5, _now.AddDays(-1));

        _context.Orders.AddRange(order1, order2, order3, order4, order5);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ExportBrandOrders_KhiKhongPhaiBrandManager_Bao403ForbiddenScopeAsync()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.BrandManager)).Returns(false);

        var service = new OrderExportService(_uow, currentUser.Object, TimeProvider.System, NullLogger<OrderExportService>.Instance);

        // Act
        var act = () => service.ExportBrandOrdersAsync(null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope && e.StatusCode == 403);
    }

    [Fact]
    public async Task ExportBrandOrders_KhoangNgayKhongHopLe_Bao400ValidationFailedAsync()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.BrandManager)).Returns(true);
        currentUser.Setup(u => u.BrandId).Returns(_brand1Id);

        var service = new OrderExportService(_uow, currentUser.Object, TimeProvider.System, NullLogger<OrderExportService>.Instance);

        // Act: from > to
        var act = () => service.ExportBrandOrdersAsync(new DateTime(2026, 9, 30), new DateTime(2026, 9, 1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ValidationFailed && e.StatusCode == 400);
    }

    [Fact]
    public async Task ExportBrandOrders_ThanhCong_TaoFileExcelDungCauTrucVaChiSoBR25Async()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.BrandManager)).Returns(true);
        currentUser.Setup(u => u.BrandId).Returns(_brand1Id);

        var service = new OrderExportService(_uow, currentUser.Object, TimeProvider.System, NullLogger<OrderExportService>.Instance);

        // Act
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 30);
        var (fileBytes, fileName) = await service.ExportBrandOrdersAsync(from, to, CancellationToken.None);

        // Assert
        fileName.Should().Be("orders_2026-09-01_2026-09-30.xlsx");
        fileBytes.Should().NotBeNullOrEmpty();

        using var ms = new MemoryStream(fileBytes);
        using var workbook = new XLWorkbook(ms);

        // 1. Kiểm tra sheet Orders
        var wsOrders = workbook.Worksheet("Orders");
        wsOrders.Should().NotBeNull();
        wsOrders.Cell(1, 1).GetString().Should().Be("OrderCode");
        wsOrders.Cell(1, 2).GetString().Should().Be("StoreName");
        wsOrders.Cell(1, 3).GetString().Should().Be("CreatedAt (UTC+7)");
        wsOrders.Cell(1, 4).GetString().Should().Be("Status");
        wsOrders.Cell(1, 5).GetString().Should().Be("RefundStatus");
        wsOrders.Cell(1, 6).GetString().Should().Be("TotalAmount");
        wsOrders.Cell(1, 7).GetString().Should().Be("PlatformFee");
        wsOrders.Cell(1, 8).GetString().Should().Be("StorePayoutAmount");
        wsOrders.Cell(1, 9).GetString().Should().Be("ItemCount");

        // Brand 1 có đúng 4 đơn (đơn của Brand 2 không có mặt - BR-11, BR-27)
        // Dòng 1 là header, các dòng 2, 3, 4, 5 là 4 đơn của Brand 1
        var rowCount = wsOrders.RowsUsed().Count();
        rowCount.Should().Be(5); // 1 header + 4 orders

        // Đơn ở chi nhánh xoá mềm vẫn có mặt (BR-22, BR-27)
        var storeNames = Enumerable.Range(2, 4).Select(r => wsOrders.Cell(r, 2).GetString()).ToList();
        storeNames.Should().Contain("Chi nhánh Đã Xoá Mềm");

        // 2. Kiểm tra sheet Summary (BR-25)
        var wsSummary = workbook.Worksheet("Summary");
        wsSummary.Should().NotBeNull();

        // Tổng doanh thu = đơn 1 (54.000) + đơn 2 (36.000) + đơn 3 (90.000) = 180.000 (đơn Cancelled không tính)
        var totalRevenue = wsSummary.Cell(3, 2).GetDouble();
        totalRevenue.Should().Be(180000);

        // Tổng số suất đã cứu = đơn 1 (2 suất) + đơn 3 (3 suất) = 5 suất (đơn NoShow và Cancelled không tính)
        var totalRescued = wsSummary.Cell(4, 2).GetDouble();
        totalRescued.Should().Be(5);
    }

    [Fact]
    public async Task ExportAdminOrders_ThanhCong_BaoGomDonCuaTatCaBrandAsync()
    {
        // Arrange: SystemAdmin
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsInRole(AppRoles.SystemAdmin)).Returns(true);

        var service = new OrderExportService(_uow, currentUser.Object, TimeProvider.System, NullLogger<OrderExportService>.Instance);

        // Act
        var (fileBytes, fileName) = await service.ExportAdminOrdersAsync(null, null, CancellationToken.None);

        // Assert
        using var ms = new MemoryStream(fileBytes);
        using var workbook = new XLWorkbook(ms);

        var wsOrders = workbook.Worksheet("Orders");
        // Toàn sàn gồm 5 đơn (Brand 1 và Brand 2)
        wsOrders.RowsUsed().Count().Should().Be(6); // 1 header + 5 orders
    }

    private static Order CreateOrder(
        Guid storeId,
        OrderStatus status,
        decimal totalAmount,
        decimal platformFee,
        decimal storePayout,
        int quantity,
        DateTime createdAt)
    {
        return new Order
        {
            OrderCode = "FR-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerUserId = Guid.NewGuid(),
            StoreId = storeId,
            SaleBatchId = Guid.NewGuid(),
            Status = status,
            RefundStatus = status == OrderStatus.Cancelled ? RefundStatus.Refunded : RefundStatus.None,
            TotalAmount = totalAmount,
            PlatformFee = platformFee,
            StorePayoutAmount = storePayout,
            CreatedAt = createdAt,
            Items =
            [
                new OrderItem
                {
                    OfferId = Guid.NewGuid(),
                    OfferNameSnapshot = "Mặt hàng mẫu",
                    UnitPrice = totalAmount / quantity,
                    Quantity = quantity,
                    LineTotal = totalAmount
                }
            ]
        };
    }
}
