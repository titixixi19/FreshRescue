using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FreshRescue.Tests.Ordering;

/// <summary>FR-S03: đặt lại tổng số suất của offer qua OrderService (07 §12.2).</summary>
public class OrderAdjustStockTests
{
    private readonly FreshRescueDbContext _context;
    private readonly Offer _offer;

    public OrderAdjustStockTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FreshRescueDbContext(options, TimeProvider.System);

        // 10 suất, đã bán / giữ chỗ 4.
        _offer = new Offer { SaleBatchId = Guid.NewGuid(), Name = "Túi mù bánh ngọt", ActualPrice = 35000, OriginalPrice = 105000, TotalQuantity = 10, RemainingQuantity = 6 };
        _context.Add(_offer);
        _context.SaveChanges();
    }

    [Theory]
    [InlineData(15, 11)]   // tăng: kho tăng cùng một lượng
    [InlineData(7, 3)]     // hạ nhưng vẫn trên số đã bán
    [InlineData(4, 0)]     // hạ về đúng số đã bán = ngừng bán (BR-24)
    public async Task AdjustOfferStock_DoiTongVaDichKhoCungMotLuongAsync(int newTotal, int expectedRemaining)
    {
        await CreateService().AdjustOfferStockAsync(_offer.Id, newTotal, CancellationToken.None);

        var offer = await _context.Offers.AsNoTracking().SingleAsync(o => o.Id == _offer.Id);
        offer.TotalQuantity.Should().Be(newTotal);
        offer.RemainingQuantity.Should().Be(expectedRemaining);
    }

    [Fact]
    public async Task AdjustOfferStock_ThapHonSoDaBan_Bao409VaKhongGhiAsync()
    {
        var act = () => CreateService().AdjustOfferStockAsync(_offer.Id, 3, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.OfferHasOrders && e.StatusCode == 409 && e.Message.Contains("4 suất"));
        var offer = await _context.Offers.AsNoTracking().SingleAsync(o => o.Id == _offer.Id);
        offer.TotalQuantity.Should().Be(10);
        offer.RemainingQuantity.Should().Be(6);
    }

    [Fact]
    public async Task AdjustOfferStock_KhongCoOffer_Bao404Async()
    {
        var act = () => CreateService().AdjustOfferStockAsync(Guid.NewGuid(), 5, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task AdjustOfferStock_SoAm_Bao400Async()
    {
        var act = () => CreateService().AdjustOfferStockAsync(_offer.Id, -1, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    private OrderService CreateService()
    {
        _context.ChangeTracker.Clear();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Offers).Returns(new InMemoryOfferRepository(_context));

        return new OrderService(
            uow.Object,
            new MapperConfiguration(c => c.AddProfile<OrderingProfile>()).CreateMapper(),
            Mock.Of<ICurrentUser>(),
            Mock.Of<IQrTicketClient>(),
            TimeProvider.System,
            NullLogger<OrderService>.Instance,
            Options.Create(new OrderingOptions()));
    }
}
