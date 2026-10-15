using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Sales;

/// <summary>Offer: thêm, đổi giá, chỉnh số lượng, xoá (BR-01, BR-07, BR-18, BR-24, OQ-14).</summary>
public class OfferServiceTests
{
    private readonly SalesFixture _f = new();

    [Fact]
    public async Task AddToBatch_HopLe_ChupLaiTemplateVaGiuNguyenTonKhoAsync()
    {
        var batch = await _f.SeedBatchAsync(withOffer: false);

        var result = await _f.OfferService(_f.StaffUser())
            .AddToBatchAsync(batch.Id, new CreateSaleBatchOfferRequest(_f.Template.Id, 30_000, 5, 2), CancellationToken.None);

        result.Name.Should().Be(_f.Template.Name);
        result.OriginalPrice.Should().Be(105_000);
        result.TotalQuantity.Should().Be(5);
        result.RemainingQuantity.Should().Be(5);
        result.MaxPerCustomer.Should().Be(2);
    }

    [Fact]
    public async Task AddToBatch_MauDaCoTrongDot_NemValidationFailedAsync()
    {
        var batch = await _f.SeedBatchAsync();

        var act = () => _f.OfferService(_f.StaffUser())
            .AddToBatchAsync(batch.Id, new CreateSaleBatchOfferRequest(_f.Template.Id, 30_000, 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task AddToBatch_MauThuocThuongHieuKhac_NemNotFoundAsync()
    {
        var batch = await _f.SeedBatchAsync(withOffer: false);
        var foreign = new FreshRescue.Entities.Catalog.OfferTemplate
        {
            BrandId = Guid.NewGuid(),
            Name = "Mẫu của brand khác",
            OfferType = OfferType.SurpriseBag,
            OriginalPrice = 105_000,
        };
        _f.Context.Add(foreign);
        await _f.Context.SaveChangesAsync();

        var act = () => _f.OfferService(_f.StaffUser())
            .AddToBatchAsync(batch.Id, new CreateSaleBatchOfferRequest(foreign.Id, 30_000, 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_DoiGia_TrongBienDo_DuocPhepAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var offer = await _f.OfferOfAsync(batch.Id);

        var result = await _f.OfferService(_f.StaffUser())
            .UpdateAsync(offer.Id, new UpdateOfferRequest(30_000, 2), CancellationToken.None);

        result.ActualPrice.Should().Be(30_000);
        result.MaxPerCustomer.Should().Be(2);
    }

    [Fact]
    public async Task Update_GiaNgoaiBienDo_NemPriceOutOfBandAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var offer = await _f.OfferOfAsync(batch.Id);

        var act = () => _f.OfferService(_f.StaffUser())
            .UpdateAsync(offer.Id, new UpdateOfferRequest(50_000, 3), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.PriceOutOfBand);
    }

    [Fact]
    public async Task Update_DoiGiaSauDonPaid_NemBatchLockedAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Active);
        var offer = await _f.OfferOfAsync(batch.Id);
        await _f.SeedOrderAsync(batch.Id, OrderStatus.Paid, paid: true, offer.Id);

        var act = () => _f.OfferService(_f.StaffUser())
            .UpdateAsync(offer.Id, new UpdateOfferRequest(30_000, 3), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
    }

    [Fact]
    public async Task AdjustQuantity_Tang_CongThemVaoConLaiAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var offer = await _f.OfferOfAsync(batch.Id);

        var result = await _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(offer.Id, new AdjustOfferQuantityRequest(15), CancellationToken.None);

        result.TotalQuantity.Should().Be(15);
        result.RemainingQuantity.Should().Be(15);
        _f.OrderService.Verify(s => s.AdjustOfferStockAsync(offer.Id, 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdjustQuantity_HaTruocKhiCoDonPaid_TruDungChenhLechAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var offer = await _f.OfferOfAsync(batch.Id);

        var result = await _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(offer.Id, new AdjustOfferQuantityRequest(6), CancellationToken.None);

        result.TotalQuantity.Should().Be(6);
        result.RemainingQuantity.Should().Be(6);
    }

    // BR-01: đã giữ chỗ 4 suất thì tổng không được thấp hơn 4, kể cả khi chưa có đơn Paid.
    [Fact]
    public async Task AdjustQuantity_HaDuoiSoDaGiuCho_NemOfferHasOrdersAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var tracked = await _f.Context.Offers.SingleAsync();
        tracked.RemainingQuantity = 6;
        await _f.Context.SaveChangesAsync();

        var act = () => _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(tracked.Id, new AdjustOfferQuantityRequest(3), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.OfferHasOrders);
        (await _f.OfferOfAsync(batch.Id)).RemainingQuantity.Should().Be(6);
    }

    // BR-24 / §6.2: đã có đơn Paid vẫn được hạ, miễn không thấp hơn số đã giữ chỗ / đã bán.
    [Fact]
    public async Task AdjustQuantity_HaSauDonPaid_DuocMienKhongThapHonSoDaBanAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Active);
        var tracked = await _f.Context.Offers.SingleAsync();
        tracked.RemainingQuantity = 6; // đã bán 4
        await _f.Context.SaveChangesAsync();
        await _f.SeedOrderAsync(batch.Id, OrderStatus.Paid, paid: true, tracked.Id);

        var down = await _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(tracked.Id, new AdjustOfferQuantityRequest(8), CancellationToken.None);
        var stop = await _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(tracked.Id, new AdjustOfferQuantityRequest(4), CancellationToken.None);
        var tooLow = () => _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(tracked.Id, new AdjustOfferQuantityRequest(3), CancellationToken.None);

        down.TotalQuantity.Should().Be(8);
        down.RemainingQuantity.Should().Be(4);
        stop.TotalQuantity.Should().Be(4);
        stop.RemainingQuantity.Should().Be(0);
        await tooLow.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.OfferHasOrders);
    }

    [Theory]
    [InlineData(SaleBatchStatus.Closed)]
    [InlineData(SaleBatchStatus.Cancelled)]
    public async Task AdjustQuantity_DotDaDongHoacHuy_NemBatchLockedAsync(SaleBatchStatus status)
    {
        var batch = await _f.SeedBatchAsync(status);
        var offer = await _f.OfferOfAsync(batch.Id);

        var act = () => _f.OfferService(_f.StaffUser())
            .AdjustQuantityAsync(offer.Id, new AdjustOfferQuantityRequest(12), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
        _f.OrderService.Verify(
            s => s.AdjustOfferStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_OfferDaCoDon_NemOfferHasOrdersAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Active);
        var offer = await _f.OfferOfAsync(batch.Id);
        await _f.SeedOrderAsync(batch.Id, OrderStatus.PendingPayment, paid: false, offer.Id);

        var act = () => _f.OfferService(_f.StaffUser()).DeleteAsync(offer.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.OfferHasOrders);
    }

    [Fact]
    public async Task Delete_OfferChuaCoDon_XoaMemAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var offer = await _f.OfferOfAsync(batch.Id);

        await _f.OfferService(_f.StaffUser()).DeleteAsync(offer.Id, CancellationToken.None);

        (await _f.Context.Offers.AsNoTracking().CountAsync()).Should().Be(0);
        (await _f.Context.Offers.IgnoreQueryFilters().AsNoTracking().SingleAsync()).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Update_OfferCuaChiNhanhKhac_NemForbiddenScopeAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var offer = await _f.OfferOfAsync(batch.Id);

        var act = () => _f.OfferService(_f.ManagerUser(brandId: Guid.NewGuid()))
            .UpdateAsync(offer.Id, new UpdateOfferRequest(30_000, 3), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope);
    }
}
