using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Sales;

/// <summary>Đợt bán: tạo Draft, sửa, kích hoạt, huỷ (FR-S02, BR-14, BR-15, BR-23, OQ-11, OQ-14).</summary>
public class SaleBatchServiceTests
{
    private readonly SalesFixture _f = new();

    private CreateSaleBatchRequest ValidRequest(
        int startInHours = 2, int saleHours = 2, decimal price = 35_000, Guid? storeId = null)
    {
        var start = SalesFixture.Now.AddHours(startInHours);
        return new CreateSaleBatchRequest(
            "Xả bánh tối",
            start,
            start.AddHours(saleHours),
            start.AddHours(1),
            start.AddHours(saleHours + 1),
            storeId,
            [new CreateSaleBatchOfferRequest(_f.Template.Id, price, 10, null)]);
    }

    [Fact]
    public async Task Create_HopLe_TaoDraftVaChupLaiDuLieuTuTemplateAsync()
    {
        var result = await _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(), CancellationToken.None);

        result.Status.Should().Be(SaleBatchStatus.Draft);
        result.StoreId.Should().Be(_f.Store.Id);
        var offer = result.Offers.Should().ContainSingle().Subject;
        offer.Name.Should().Be(_f.Template.Name);
        offer.OriginalPrice.Should().Be(105_000);
        offer.ActualPrice.Should().Be(35_000);
        offer.TotalQuantity.Should().Be(10);
        offer.RemainingQuantity.Should().Be(10);
        offer.MaxPerCustomer.Should().Be(3);
        (await _f.Context.SaleBatches.CountAsync()).Should().Be(1);
    }

    // BR-15: SaleStart < SaleEnd ≤ PickupEnd, PickupStart < PickupEnd, SaleStart ≤ PickupStart.
    [Theory]
    [InlineData(0, 0, 1, 3)]   // SaleStart == SaleEnd
    [InlineData(0, 2, 1, 1)]   // PickupStart == PickupEnd
    [InlineData(0, 4, 1, 3)]   // SaleEnd > PickupEnd
    [InlineData(2, 3, 1, 4)]   // SaleStart > PickupStart
    public async Task Create_MocThoiGianSai_NemBatchTimeInvalidAsync(int saleStart, int saleEnd, int pickupStart, int pickupEnd)
    {
        var baseTime = SalesFixture.Now.AddHours(2);
        var request = ValidRequest() with
        {
            SaleStartTime = baseTime.AddHours(saleStart),
            SaleEndTime = baseTime.AddHours(saleEnd),
            PickupStartTime = baseTime.AddHours(pickupStart),
            PickupEndTime = baseTime.AddHours(pickupEnd),
        };

        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchTimeInvalid);
    }

    [Fact]
    public async Task Create_ChongKhungGioBanCuaDotKhac_NemBatchOverlapAsync()
    {
        await _f.SeedBatchAsync(SaleBatchStatus.Scheduled, startInHours: 2, saleHours: 2);

        // Đợt mới bắt đầu 1 giờ sau khi đợt cũ mở, nằm lọt trong khung của đợt cũ.
        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(startInHours: 3), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchOverlap);
    }

    [Fact]
    public async Task Create_SatBienKhongChongKhungGio_DuocPhepAsync()
    {
        await _f.SeedBatchAsync(SaleBatchStatus.Scheduled, startInHours: 2, saleHours: 2);

        // Đợt cũ bán tới đúng lúc đợt mới bắt đầu: [a, b) không giao nhau.
        var result = await _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(startInHours: 4), CancellationToken.None);

        result.Status.Should().Be(SaleBatchStatus.Draft);
    }

    [Fact]
    public async Task Create_DotKhacDaHuy_KhongTinhVaoChongGioAsync()
    {
        await _f.SeedBatchAsync(SaleBatchStatus.Cancelled, startInHours: 2, saleHours: 2);

        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(startInHours: 2), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(26_249)]
    [InlineData(36_751)]
    public async Task Create_GiaNgoaiBienDoSan_NemPriceOutOfBandAsync(decimal price)
    {
        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(price: price), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.PriceOutOfBand);
    }

    [Theory]
    [InlineData(26_250)]
    [InlineData(36_750)]
    public async Task Create_GiaDungBienDoSan_DuocPhepAsync(decimal price)
    {
        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(price: price), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Create_BrandSietChatHonSan_ChiChapNhanGiaTrongGiaoAsync()
    {
        // Tầng brand 70–72% thu hẹp khoảng 65–75% của sàn: giá hợp lệ 29.400–31.500đ.
        var template = await _f.Context.OfferTemplates.SingleAsync();
        template.BrandMinDiscountPercent = 70;
        template.BrandMaxDiscountPercent = 72;
        await _f.Context.SaveChangesAsync();

        var tooCheap = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(price: 28_000), CancellationToken.None);
        var ok = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(price: 30_000), CancellationToken.None);

        await tooCheap.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.PriceOutOfBand);
        await ok.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(BrandStatus.PendingApproval)]
    [InlineData(BrandStatus.Rejected)]
    [InlineData(BrandStatus.Suspended)]
    public async Task Create_ThuongHieuChuaDuyet_NemBrandNotApprovedAsync(BrandStatus status)
    {
        var brand = await _f.Context.Brands.SingleAsync();
        brand.Status = status;
        await _f.Context.SaveChangesAsync();

        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BrandNotApproved);
    }

    // Chi nhánh Inactive là trạng thái nghiệp vụ, không phải lỗi định dạng: 409 BATCH_LOCKED.
    [Fact]
    public async Task Create_ChiNhanhInactive_NemBatchLockedAsync()
    {
        var store = await _f.Context.Stores.SingleAsync();
        store.Status = StoreStatus.Inactive;
        await _f.Context.SaveChangesAsync();

        var act = () => _f.BatchService(_f.StaffUser()).CreateAsync(ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
    }

    [Fact]
    public async Task Create_BrandManagerThieuStoreId_NemValidationFailedAsync()
    {
        var act = () => _f.BatchService(_f.ManagerUser()).CreateAsync(ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Create_BrandManagerDungStoreCuaMinh_DuocPhepAsync()
    {
        var result = await _f.BatchService(_f.ManagerUser())
            .CreateAsync(ValidRequest(storeId: _f.Store.Id), CancellationToken.None);

        result.StoreId.Should().Be(_f.Store.Id);
    }

    [Fact]
    public async Task Create_BrandManagerThuongHieuKhac_NemForbiddenScopeAsync()
    {
        var act = () => _f.BatchService(_f.ManagerUser(brandId: Guid.NewGuid()))
            .CreateAsync(ValidRequest(storeId: _f.Store.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task Update_SauDonPaidDauTien_DoiMocThoiGianBiKhoaAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Scheduled);
        await _f.SeedOrderAsync(batch.Id, OrderStatus.Paid, paid: true);

        var request = new UpdateSaleBatchRequest(
            batch.Name, batch.SaleStartTime, batch.SaleEndTime, batch.PickupStartTime, batch.PickupEndTime.AddHours(1));

        var act = () => _f.BatchService(_f.StaffUser()).UpdateAsync(batch.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
    }

    [Fact]
    public async Task Update_SauDonPaid_ChiDoiTenVanDuocAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Scheduled);
        await _f.SeedOrderAsync(batch.Id, OrderStatus.Paid, paid: true);

        var request = new UpdateSaleBatchRequest(
            "Tên mới", batch.SaleStartTime, batch.SaleEndTime, batch.PickupStartTime, batch.PickupEndTime);

        var result = await _f.BatchService(_f.StaffUser()).UpdateAsync(batch.Id, request, CancellationToken.None);

        result.Name.Should().Be("Tên mới");
    }

    // OQ-14: mới chỉ có đơn PendingPayment thì vẫn được lùi giờ.
    [Fact]
    public async Task Update_ChiCoDonChuaThanhToan_VanDuocDoiMocAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Scheduled);
        await _f.SeedOrderAsync(batch.Id, OrderStatus.PendingPayment, paid: false);

        var request = new UpdateSaleBatchRequest(
            batch.Name, batch.SaleStartTime, batch.SaleEndTime, batch.PickupStartTime, batch.PickupEndTime.AddHours(1));

        var result = await _f.BatchService(_f.StaffUser()).UpdateAsync(batch.Id, request, CancellationToken.None);

        result.PickupEndTime.Should().Be(batch.PickupEndTime.AddHours(1));
    }

    [Theory]
    [InlineData(SaleBatchStatus.Closed)]
    [InlineData(SaleBatchStatus.Cancelled)]
    public async Task Update_DotDaDongHoacHuy_NemBatchLockedAsync(SaleBatchStatus status)
    {
        var batch = await _f.SeedBatchAsync(status);
        var request = new UpdateSaleBatchRequest(
            "x", batch.SaleStartTime, batch.SaleEndTime, batch.PickupStartTime, batch.PickupEndTime);

        var act = () => _f.BatchService(_f.StaffUser()).UpdateAsync(batch.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
    }

    [Fact]
    public async Task Update_DoiMocLamChongDotKhac_NemBatchOverlapAsync()
    {
        await _f.SeedBatchAsync(SaleBatchStatus.Scheduled, startInHours: 2, saleHours: 2);
        var other = await _f.SeedBatchAsync(SaleBatchStatus.Draft, startInHours: 5, saleHours: 2);

        var start = SalesFixture.Now.AddHours(3);
        var request = new UpdateSaleBatchRequest(other.Name, start, start.AddHours(2), start.AddHours(1), start.AddHours(3));

        var act = () => _f.BatchService(_f.StaffUser()).UpdateAsync(other.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchOverlap);
    }

    [Fact]
    public async Task Activate_DotDraft_ChuyenSangScheduledAsync()
    {
        var batch = await _f.SeedBatchAsync();

        var result = await _f.BatchService(_f.StaffUser()).ActivateAsync(batch.Id, CancellationToken.None);

        result.Status.Should().Be(SaleBatchStatus.Scheduled);
        // ActivatedAt chỉ ghi lúc job FR-X01 thật sự mở đợt.
        result.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Activate_DotChuaCoOffer_NemValidationFailedAsync()
    {
        var batch = await _f.SeedBatchAsync(withOffer: false);

        var act = () => _f.BatchService(_f.StaffUser()).ActivateAsync(batch.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Activate_DotKhongPhaiDraft_NemBatchLockedAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Scheduled);

        var act = () => _f.BatchService(_f.StaffUser()).ActivateAsync(batch.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
    }

    [Fact]
    public async Task Activate_KhungGioBanDaQua_NemBatchTimeInvalidAsync()
    {
        var batch = await _f.SeedBatchAsync(startInHours: -5, saleHours: 2);

        var act = () => _f.BatchService(_f.StaffUser()).ActivateAsync(batch.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchTimeInvalid);
    }

    [Fact]
    public async Task Cancel_StoreStaff_NemForbiddenAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Scheduled);

        var act = () => _f.BatchService(_f.StaffUser())
            .CancelAsync(batch.Id, new CancelSaleBatchRequest("Lò hỏng"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope);
        _f.OrderService.Verify(
            o => o.CancelBatchOrdersAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Cancel_BrandManager_HuyDotVaGoiHuyDonDungMotLanAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Active);
        _f.OrderService
            .Setup(o => o.CancelBatchOrdersAsync(batch.Id, "Lò hỏng", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _f.BatchService(_f.ManagerUser())
            .CancelAsync(batch.Id, new CancelSaleBatchRequest("  Lò hỏng "), CancellationToken.None);

        result.Status.Should().Be(SaleBatchStatus.Cancelled);
        result.CancelReason.Should().Be("Lò hỏng");
        _f.OrderService.Verify(
            o => o.CancelBatchOrdersAsync(batch.Id, "Lò hỏng", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_DotDaCoDonDaTraoHang_NemVaGiuNguyenTrangThaiAsync()
    {
        var batch = await _f.SeedBatchAsync(SaleBatchStatus.Active);
        _f.OrderService
            .Setup(o => o.CancelBatchOrdersAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.Conflict(ErrorCodes.BatchHasRedeemedOrders, "đã trao hàng"));

        var act = () => _f.BatchService(_f.ManagerUser())
            .CancelAsync(batch.Id, new CancelSaleBatchRequest("x"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchHasRedeemedOrders);
        (await _f.Context.SaleBatches.AsNoTracking().SingleAsync()).Status.Should().Be(SaleBatchStatus.Active);
    }

    [Theory]
    [InlineData(SaleBatchStatus.Closed)]
    [InlineData(SaleBatchStatus.Cancelled)]
    public async Task Cancel_DotDaDongHoacHuy_NemBatchLockedAsync(SaleBatchStatus status)
    {
        var batch = await _f.SeedBatchAsync(status);

        var act = () => _f.BatchService(_f.ManagerUser())
            .CancelAsync(batch.Id, new CancelSaleBatchRequest("x"), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.BatchLocked);
    }

    [Fact]
    public async Task GetById_DotCuaChiNhanhKhac_NemForbiddenScopeAsync()
    {
        var batch = await _f.SeedBatchAsync();
        var otherStaff = new Mock<ICurrentUser>();
        otherStaff.Setup(u => u.IsInRole(FreshRescue.Entities.Constants.AppRoles.StoreStaff)).Returns(true);
        otherStaff.Setup(u => u.StoreId).Returns(Guid.NewGuid());

        var act = () => _f.BatchService(otherStaff).GetByIdAsync(batch.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task List_ChiTraDotTrongPhamViVaLocTheoTrangThaiAsync()
    {
        await _f.SeedBatchAsync(SaleBatchStatus.Draft, startInHours: 2, saleHours: 1);
        await _f.SeedBatchAsync(SaleBatchStatus.Scheduled, startInHours: 6, saleHours: 1);

        var otherStore = new FreshRescue.Entities.Catalog.Store { BrandId = _f.BrandId, Name = "Chi nhánh khác" };
        _f.Context.Add(otherStore);
        _f.Context.Add(new FreshRescue.Entities.Sales.SaleBatch
        {
            StoreId = otherStore.Id,
            Status = SaleBatchStatus.Draft,
            SaleStartTime = SalesFixture.Now.AddHours(10),
            SaleEndTime = SalesFixture.Now.AddHours(11),
            PickupStartTime = SalesFixture.Now.AddHours(10),
            PickupEndTime = SalesFixture.Now.AddHours(12),
        });
        await _f.Context.SaveChangesAsync();

        var staffAll = await _f.BatchService(_f.StaffUser()).ListAsync(new SaleBatchListQuery(), CancellationToken.None);
        var managerAll = await _f.BatchService(_f.ManagerUser()).ListAsync(new SaleBatchListQuery(), CancellationToken.None);
        var staffScheduled = await _f.BatchService(_f.StaffUser())
            .ListAsync(new SaleBatchListQuery { Status = SaleBatchStatus.Scheduled }, CancellationToken.None);

        staffAll.TotalCount.Should().Be(2);
        managerAll.TotalCount.Should().Be(3);
        staffScheduled.Items.Should().ContainSingle();
    }
}
