using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Tests.Catalog;

/// <summary><c>GET /stores/nearby</c> và <c>GET /stores/{id}</c> (FR-C02, 02-DOMAIN-MODEL.md §5.1).</summary>
public class StorefrontStoreTests
{
    // Người dùng đứng ở Quận 10 (TP.HCM) — các điểm dưới đây tính khoảng cách từ đây.
    private const decimal UserLat = 10.0m;
    private const decimal UserLng = 106.0m;

    private readonly StorefrontFixture _f = new();

    private static StoreNearbyQuery Near(decimal radiusKm = 5, bool hasActiveBatch = false, Guid? categoryId = null)
        => new() { Lat = UserLat, Lng = UserLng, RadiusKm = radiusKm, HasActiveBatch = hasActiveBatch, CategoryId = categoryId };

    [Fact]
    public async Task Nearby_SapGanNhatTruocVaTinhDistanceKmAsync()
    {
        var far = _f.AddStore("Xa", 10.03m, 106.03m);
        var near = _f.AddStore("Gần", 10.01m, 106.0m);

        var result = await _f.Service().FindNearbyStoresAsync(Near(), CancellationToken.None);

        result.Items.Select(s => s.Id).Should().Equal(near.Id, far.Id);
        result.Items[0].DistanceKm.Should().BeApproximately(1.11m, 0.02m);
        result.Items[0].BrandName.Should().Be("Tiệm bánh Mơ");
    }

    [Fact]
    public async Task Nearby_NgoaiBanKinh_BiLoaiAsync()
    {
        _f.AddStore("Cách 11km", 10.1m, 106.0m);

        var result = await _f.Service().FindNearbyStoresAsync(Near(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Nearby_GocHopBaoNamNgoaiHinhTron_BiLoaiAsync()
    {
        // Nằm trong hộp bao ±5km nhưng đường chim bay ≈ 6,2 km > 5 km: chỉ Haversine mới loại được.
        var corner = _f.AddStore("Góc hộp", 10.04m, 106.04m);
        var inside = _f.AddStore("Trong vòng", 10.02m, 106.02m);

        var result = await _f.Service().FindNearbyStoresAsync(Near(), CancellationToken.None);

        result.Items.Select(s => s.Id).Should().Equal(inside.Id);
        result.Items.Should().NotContain(s => s.Id == corner.Id);
    }

    [Fact]
    public async Task Nearby_BoQuaChiNhanhNgungHoatDongAsync()
    {
        _f.AddStore("Tắt", 10.01m, 106.0m, StoreStatus.Inactive);
        var active = _f.AddStore("Mở", 10.01m, 106.0m);

        var result = await _f.Service().FindNearbyStoresAsync(Near(), CancellationToken.None);

        result.Items.Select(s => s.Id).Should().Equal(active.Id);
    }

    [Fact]
    public async Task Nearby_HasActiveBatch_ChiLayChiNhanhDangMoBanAsync()
    {
        var selling = _f.AddStore("Đang bán", 10.01m, 106.0m);
        var idle = _f.AddStore("Không bán", 10.005m, 106.0m);
        var expired = _f.AddStore("Hết giờ", 10.002m, 106.0m);
        _f.AddOffer(_f.AddBatch(selling));
        _f.AddOffer(_f.AddBatch(expired, SaleBatchStatus.Active, -3, -1));

        var all = await _f.Service().FindNearbyStoresAsync(Near(), CancellationToken.None);
        var onlyOpen = await _f.Service().FindNearbyStoresAsync(Near(hasActiveBatch: true), CancellationToken.None);

        all.Items.Select(s => s.Id).Should().BeEquivalentTo([selling.Id, idle.Id, expired.Id]);
        onlyOpen.Items.Select(s => s.Id).Should().Equal(selling.Id);
    }

    [Fact]
    public async Task Nearby_DemDotMoVaGiaThapNhatTrongOfferConSuatAsync()
    {
        var store = _f.AddStore("Có đợt", 10.01m, 106.0m);
        var batch = _f.AddBatch(store);
        _f.AddOffer(batch, "Rẻ nhưng hết", actualPrice: 10_000, remaining: 0);
        _f.AddOffer(batch, "Còn suất", actualPrice: 29_000, remaining: 3);
        _f.AddOffer(batch, "Đắt hơn", actualPrice: 45_000, remaining: 3);
        _f.AddBatch(store, SaleBatchStatus.Draft);
        _f.AddStore("Không đợt", 10.02m, 106.0m);

        var result = await _f.Service().FindNearbyStoresAsync(Near(), CancellationToken.None);

        var withBatch = result.Items.Single(s => s.Id == store.Id);
        withBatch.ActiveBatchCount.Should().Be(1);
        withBatch.MinPrice.Should().Be(29_000);

        var without = result.Items.Single(s => s.Name == "Không đợt");
        without.ActiveBatchCount.Should().Be(0);
        without.MinPrice.Should().BeNull();
    }

    [Fact]
    public async Task Nearby_LocTheoDanhMucAsync()
    {
        var bakery = _f.AddStore("Bánh ngọt", 10.01m, 106.0m);
        var savory = _f.AddStore("Bánh mặn", 10.02m, 106.0m);
        _f.AddOffer(_f.AddBatch(bakery), category: _f.Bakery);
        _f.AddOffer(_f.AddBatch(savory), category: _f.Savory);

        var result = await _f.Service().FindNearbyStoresAsync(Near(categoryId: _f.Savory.Id), CancellationToken.None);

        result.Items.Select(s => s.Id).Should().Equal(savory.Id);
    }

    [Fact]
    public async Task Nearby_PhanTrangSauKhiSapXepTheoKhoangCachAsync()
    {
        _f.AddStore("1", 10.005m, 106.0m);
        var second = _f.AddStore("2", 10.01m, 106.0m);
        _f.AddStore("3", 10.02m, 106.0m);

        var result = await _f.Service().FindNearbyStoresAsync(
            Near() with { Page = 2, PageSize = 1 }, CancellationToken.None);

        result.Items.Single().Id.Should().Be(second.Id);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetStore_TraChiTietKemDotDangMoAsync()
    {
        var store = _f.AddStore("Nguyễn Huệ");
        var open = _f.AddBatch(store);
        var offer = _f.AddOffer(open);
        _f.AddOffer(_f.AddBatch(store, SaleBatchStatus.Draft), "Nháp");
        _f.AddOffer(_f.AddBatch(store, SaleBatchStatus.Active, -3, -1), "Hết giờ");
        _f.AddOffer(open, "Đã xoá", deleted: true);

        var dto = await _f.Service().GetStoreAsync(store.Id, CancellationToken.None);

        dto.Name.Should().Be("Nguyễn Huệ");
        dto.BrandName.Should().Be("Tiệm bánh Mơ");
        var batch = dto.ActiveBatches.Should().ContainSingle().Subject;
        batch.Id.Should().Be(open.Id);
        batch.Offers.Select(o => o.Id).Should().Equal(offer.Id);
        batch.Offers[0].StoreName.Should().Be("Nguyễn Huệ");
    }

    [Fact]
    public async Task GetStore_ChiNhanhKhongCoDotMo_TraDanhSachRongAsync()
    {
        var store = _f.AddStore();

        var dto = await _f.Service().GetStoreAsync(store.Id, CancellationToken.None);

        dto.ActiveBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStore_KhongTonTai_Nem404Async()
    {
        var act = () => _f.Service().GetStoreAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetStore_NgungHoatDong_Nem404Async()
    {
        var store = _f.AddStore(status: StoreStatus.Inactive);

        var act = () => _f.Service().GetStoreAsync(store.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }
}

public class GeoDistanceTests
{
    [Fact]
    public void Haversine_MotDoVi_XapXi111Km()
    {
        GeoDistance.HaversineKm(0, 0, 1, 0).Should().BeApproximately(111.19, 0.05);
    }

    [Fact]
    public void Haversine_CungDiem_BangKhong()
    {
        GeoDistance.HaversineKm(10.7741, 106.7035, 10.7741, 106.7035).Should().Be(0);
    }

    [Fact]
    public void Haversine_HaNoiDenTpHcm_XapXi1160Km()
    {
        GeoDistance.HaversineKm(21.0278, 105.8342, 10.8231, 106.6297).Should().BeApproximately(1138, 15);
    }

    [Fact]
    public void BoundingBox_ChuaDiemCachDung5Km()
    {
        var box = GeoDistance.BoundingBox(10.0, 106.0, 5);

        // 5 km về phía bắc / đông vẫn nằm trong hộp.
        box.MaxLat.Should().BeGreaterThan(10.0m + 0.044m);
        box.MaxLng.Should().BeGreaterThan(106.0m + 0.045m);
        box.MinLat.Should().BeLessThan(10.0m - 0.044m);
        box.MinLng.Should().BeLessThan(106.0m - 0.045m);
    }

    [Fact]
    public void BoundingBox_GanCuc_PhuHetKinhDo()
    {
        var box = GeoDistance.BoundingBox(89.9, 0, 5);

        box.MinLng.Should().Be(-180m);
        box.MaxLng.Should().Be(180m);
    }
}
