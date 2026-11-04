using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Tests.Catalog;

/// <summary><c>GET /offers/active</c> và <c>GET /offers/{id}</c> (FR-C03).</summary>
public class StorefrontOfferTests
{
    private readonly StorefrontFixture _f = new();

    [Fact]
    public async Task ListActive_ChiLayOfferCuaDotActiveTrongKhungGioBanAsync()
    {
        var store = _f.AddStore();
        var open = _f.AddOffer(_f.AddBatch(store), name: "Đang bán");
        _f.AddOffer(_f.AddBatch(store, SaleBatchStatus.Draft), name: "Bản nháp");
        _f.AddOffer(_f.AddBatch(store, SaleBatchStatus.Scheduled, 2, 3), name: "Chưa tới giờ");
        _f.AddOffer(_f.AddBatch(store, SaleBatchStatus.Active, -3, -1), name: "Hết giờ bán");
        _f.AddOffer(_f.AddBatch(store, SaleBatchStatus.Cancelled), name: "Đã huỷ");

        var result = await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery(), CancellationToken.None);

        result.Items.Select(o => o.Id).Should().Equal(open.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListActive_BoQuaOfferDaXoaMemAsync()
    {
        var batch = _f.AddBatch(_f.AddStore());
        var kept = _f.AddOffer(batch, name: "Còn");
        _f.AddOffer(batch, name: "Đã xoá", deleted: true);

        var result = await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery(), CancellationToken.None);

        result.Items.Select(o => o.Id).Should().Equal(kept.Id);
    }

    [Fact]
    public async Task ListActive_ChiNhanhNgungHoatDongHoacThuongHieuChuaDuyet_BiAnAsync()
    {
        var pending = new FreshRescue.Entities.Catalog.Brand
        {
            Name = "Chưa duyệt",
            Slug = "chua-duyet",
            Status = BrandStatus.PendingApproval,
        };
        _f.Context.Add(pending);
        _f.Context.SaveChanges();

        _f.AddOffer(_f.AddBatch(_f.AddStore(status: StoreStatus.Inactive)), name: "Chi nhánh tắt");
        _f.AddOffer(_f.AddBatch(_f.AddStore(brand: pending)), name: "Brand chờ duyệt");

        var result = await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListActive_LocTheoStoreIdAsync()
    {
        var a = _f.AddStore("A");
        var b = _f.AddStore("B");
        var offerA = _f.AddOffer(_f.AddBatch(a));
        _f.AddOffer(_f.AddBatch(b));

        var result = await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery { StoreId = a.Id }, CancellationToken.None);

        result.Items.Select(o => o.Id).Should().Equal(offerA.Id);
        result.Items[0].StoreName.Should().Be("A");
    }

    [Fact]
    public async Task ListActive_LocTheoDanhMucVaLoaiAsync()
    {
        var batch = _f.AddBatch(_f.AddStore());
        _f.AddOffer(batch, "Bánh ngọt thường", category: _f.Bakery, type: OfferType.ClearanceItem);
        var target = _f.AddOffer(batch, "Túi mù mặn", category: _f.Savory, type: OfferType.SurpriseBag);

        var byCategory = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { CategoryId = _f.Savory.Id }, CancellationToken.None);
        var byType = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { OfferType = OfferType.SurpriseBag }, CancellationToken.None);

        byCategory.Items.Select(o => o.Id).Should().Equal(target.Id);
        byType.Items.Select(o => o.Id).Should().Equal(target.Id);
        byCategory.Items[0].CategoryName.Should().Be("Bánh mặn");
    }

    [Fact]
    public async Task ListActive_LocTheoKhoangGia_BaoGomHaiDauAsync()
    {
        var batch = _f.AddBatch(_f.AddStore());
        _f.AddOffer(batch, "Rẻ", actualPrice: 20_000);
        var low = _f.AddOffer(batch, "Vừa thấp", actualPrice: 30_000);
        var high = _f.AddOffer(batch, "Vừa cao", actualPrice: 40_000);
        _f.AddOffer(batch, "Đắt", actualPrice: 50_000);

        var result = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { MinPrice = 30_000, MaxPrice = 40_000 }, CancellationToken.None);

        result.Items.Select(o => o.Id).Should().BeEquivalentTo([low.Id, high.Id]);
    }

    [Fact]
    public async Task ListActive_InStockOnly_BoOfferHetSuatAsync()
    {
        var batch = _f.AddBatch(_f.AddStore());
        var available = _f.AddOffer(batch, "Còn suất", remaining: 2);
        _f.AddOffer(batch, "Hết suất", remaining: 0);

        var all = await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery(), CancellationToken.None);
        var inStock = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { InStockOnly = true }, CancellationToken.None);

        all.TotalCount.Should().Be(2);
        inStock.Items.Select(o => o.Id).Should().Equal(available.Id);
    }

    [Fact]
    public async Task ListActive_TinhPhanTramGiamVaTraKhungGioAsync()
    {
        var batch = _f.AddBatch(_f.AddStore());
        _f.AddOffer(batch, actualPrice: 35_000);

        var dto = (await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery(), CancellationToken.None)).Items.Single();

        dto.DiscountPercent.Should().Be(65m);
        dto.SaleEndTime.Should().Be(batch.SaleEndTime);
        dto.PickupStartTime.Should().Be(batch.PickupStartTime);
    }

    [Fact]
    public async Task ListActive_PhanTrangVaSapTheoHanBanAsync()
    {
        var store = _f.AddStore();
        var later = _f.AddOffer(_f.AddBatch(store, endHoursFromNow: 3), "Muộn");
        var sooner = _f.AddOffer(_f.AddBatch(store, endHoursFromNow: 1), "Sớm");

        var page1 = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { Page = 1, PageSize = 1 }, CancellationToken.None);
        var page2 = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { Page = 2, PageSize = 1 }, CancellationToken.None);

        page1.Items.Single().Id.Should().Be(sooner.Id);
        page2.Items.Single().Id.Should().Be(later.Id);
        page1.TotalCount.Should().Be(2);
        page1.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetOffer_DotActive_TraChiTietAsync()
    {
        var offer = _f.AddOffer(_f.AddBatch(_f.AddStore("Nguyễn Huệ")));

        var dto = await _f.Service().GetOfferAsync(offer.Id, CancellationToken.None);

        dto.Id.Should().Be(offer.Id);
        dto.StoreName.Should().Be("Nguyễn Huệ");
        dto.CategoryName.Should().Be("Bánh ngọt");
    }

    [Fact]
    public async Task GetOffer_KhongTonTai_Nem404Async()
    {
        var act = () => _f.Service().GetOfferAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.NotFound);
    }

    [Theory]
    [InlineData(SaleBatchStatus.Draft)]
    [InlineData(SaleBatchStatus.Scheduled)]
    [InlineData(SaleBatchStatus.Closed)]
    [InlineData(SaleBatchStatus.Cancelled)]
    public async Task GetOffer_DotKhongActive_Nem404Async(SaleBatchStatus status)
    {
        var offer = _f.AddOffer(_f.AddBatch(_f.AddStore(), status));

        var act = () => _f.Service().GetOfferAsync(offer.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task GetOffer_DaXoaMem_Nem404Async()
    {
        var offer = _f.AddOffer(_f.AddBatch(_f.AddStore()), deleted: true);

        var act = () => _f.Service().GetOfferAsync(offer.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task ListActive_CoToaDo_LocTheoBanKinhVaSapGanNhatTruocAsync()
    {
        var near = _f.AddOffer(_f.AddBatch(_f.AddStore("Gần", 10.01m, 106.0m)), name: "Gần");
        var mid = _f.AddOffer(_f.AddBatch(_f.AddStore("Vừa", 10.03m, 106.03m)), name: "Vừa");
        _f.AddOffer(_f.AddBatch(_f.AddStore("Cách 11km", 10.1m, 106.0m)), name: "Xa");

        var result = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { Lat = 10.0m, Lng = 106.0m, RadiusKm = 5 }, CancellationToken.None);

        result.Items.Select(o => o.Id).Should().Equal(near.Id, mid.Id);
        result.TotalCount.Should().Be(2);
        result.Items[0].DistanceKm.Should().BeApproximately(1.11m, 0.02m);
    }

    [Fact]
    public async Task ListActive_KhongCoToaDo_KhongTraDistanceKmAsync()
    {
        _f.AddOffer(_f.AddBatch(_f.AddStore()));

        var result = await _f.Service().ListActiveOffersAsync(new ActiveOfferListQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle().Which.DistanceKm.Should().BeNull();
    }

    [Fact]
    public async Task ListActive_CoToaDo_PhanTrangTheoKhoangCachAsync()
    {
        var first = _f.AddOffer(_f.AddBatch(_f.AddStore("1", 10.01m, 106.0m)), name: "1");
        var second = _f.AddOffer(_f.AddBatch(_f.AddStore("2", 10.02m, 106.0m)), name: "2");
        var third = _f.AddOffer(_f.AddBatch(_f.AddStore("3", 10.03m, 106.0m)), name: "3");

        var page1 = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { Lat = 10.0m, Lng = 106.0m, Page = 1, PageSize = 2 }, CancellationToken.None);
        var page2 = await _f.Service().ListActiveOffersAsync(
            new ActiveOfferListQuery { Lat = 10.0m, Lng = 106.0m, Page = 2, PageSize = 2 }, CancellationToken.None);

        page1.Items.Select(o => o.Id).Should().Equal(first.Id, second.Id);
        page2.Items.Select(o => o.Id).Should().Equal(third.Id);
        page2.TotalCount.Should().Be(3);
        page2.TotalPages.Should().Be(2);
    }
}
