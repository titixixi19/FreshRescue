using FluentAssertions;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Business.Features.Catalog.Validators;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Tests.Catalog;

public class StorefrontValidatorsTests
{
    private readonly ActiveOfferListQueryValidator _offers = new();
    private readonly StoreNearbyQueryValidator _nearby = new();

    private static StoreNearbyQuery ValidNearby() => new() { Lat = 10.77m, Lng = 106.70m };

    [Fact]
    public void ActiveOffers_MacDinh_HopLe()
    {
        _offers.Validate(new ActiveOfferListQuery()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ActiveOffers_GiaAm_Loi()
    {
        _offers.Validate(new ActiveOfferListQuery { MinPrice = -1 }).IsValid.Should().BeFalse();
        _offers.Validate(new ActiveOfferListQuery { MaxPrice = -1 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActiveOffers_MaxNhoHonMin_Loi()
    {
        _offers.Validate(new ActiveOfferListQuery { MinPrice = 50_000, MaxPrice = 10_000 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActiveOffers_OfferTypeNgoaiEnum_Loi()
    {
        _offers.Validate(new ActiveOfferListQuery { OfferType = (OfferType)99 }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ActiveOffers_PhanTrangSai_Loi(int page, int pageSize)
    {
        _offers.Validate(new ActiveOfferListQuery { Page = page, PageSize = pageSize }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Nearby_HopLe_KhongLoi()
    {
        _nearby.Validate(ValidNearby()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Nearby_MacDinhBanKinh5Km()
    {
        ValidNearby().RadiusKm.Should().Be(5);
    }

    [Fact]
    public void Nearby_ThieuLatHoacLng_Loi()
    {
        _nearby.Validate(new StoreNearbyQuery { Lng = 106.7m }).IsValid.Should().BeFalse();
        _nearby.Validate(new StoreNearbyQuery { Lat = 10.7m }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(91, 106)]
    [InlineData(-91, 106)]
    [InlineData(10, 181)]
    [InlineData(10, -181)]
    public void Nearby_ToaDoNgoaiKhoang_Loi(double lat, double lng)
    {
        _nearby.Validate(new StoreNearbyQuery { Lat = (decimal)lat, Lng = (decimal)lng }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(50.1)]
    public void Nearby_BanKinhNgoaiKhoang_Loi(double radius)
    {
        _nearby.Validate(ValidNearby() with { RadiusKm = (decimal)radius }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Nearby_BanKinhToiDa50Km_HopLe()
    {
        _nearby.Validate(ValidNearby() with { RadiusKm = 50 }).IsValid.Should().BeTrue();
    }
}
