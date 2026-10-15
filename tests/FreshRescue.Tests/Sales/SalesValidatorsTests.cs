using FluentAssertions;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.Business.Features.Sales.Validators;

namespace FreshRescue.Tests.Sales;

public class SalesValidatorsTests
{
    private static readonly DateTime _t = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    private static CreateSaleBatchRequest Request(List<CreateSaleBatchOfferRequest> offers, string? name = "Đợt tối")
        => new(name, _t, _t.AddHours(2), _t.AddHours(1), _t.AddHours(3), null, offers);

    private static CreateSaleBatchOfferRequest Line(Guid? id = null, decimal price = 35_000, int qty = 5, int? max = null)
        => new(id ?? Guid.NewGuid(), price, qty, max);

    [Fact]
    public void CreateSaleBatch_HopLe_KhongLoi()
    {
        new CreateSaleBatchRequestValidator().Validate(Request([Line()])).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateSaleBatch_KhongCoOffer_Loi()
    {
        new CreateSaleBatchRequestValidator().Validate(Request([])).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateSaleBatch_HaiDongCungMau_Loi()
    {
        var id = Guid.NewGuid();

        new CreateSaleBatchRequestValidator().Validate(Request([Line(id), Line(id)])).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateSaleBatch_QuaMuoiMatHang_Loi()
    {
        var lines = Enumerable.Range(0, CreateSaleBatchRequestValidator.MaxOffers + 1).Select(_ => Line()).ToList();

        new CreateSaleBatchRequestValidator().Validate(Request(lines)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateSaleBatch_TenQua150KyTu_Loi()
    {
        new CreateSaleBatchRequestValidator().Validate(Request([Line()], new string('a', 151))).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 5, null)]
    [InlineData(-1, 5, null)]
    [InlineData(35_000, 0, null)]
    [InlineData(35_000, 10_001, null)]
    [InlineData(35_000, 5, 0)]
    [InlineData(35_000, 5, 11)]
    public void CreateOffer_GiaTriNgoaiKhoang_Loi(decimal price, int qty, int? max)
    {
        new CreateSaleBatchOfferRequestValidator().Validate(Line(price: price, qty: qty, max: max)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Lò hỏng", true)]
    public void CancelSaleBatch_LyDoBatBuoc(string reason, bool valid)
    {
        new CancelSaleBatchRequestValidator().Validate(new CancelSaleBatchRequest(reason)).IsValid.Should().Be(valid);
    }

    [Fact]
    public void CancelSaleBatch_LyDoQua500KyTu_Loi()
    {
        new CancelSaleBatchRequestValidator().Validate(new CancelSaleBatchRequest(new string('a', 501))).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(10_000, true)]
    [InlineData(-1, false)]
    [InlineData(10_001, false)]
    public void AdjustQuantity_KhoangHopLe(int total, bool valid)
    {
        new AdjustOfferQuantityRequestValidator().Validate(new AdjustOfferQuantityRequest(total)).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void SaleBatchListQuery_PageVaPageSize(int page, int pageSize, bool valid)
    {
        var query = new SaleBatchListQuery { Page = page, PageSize = pageSize };

        new SaleBatchListQueryValidator().Validate(query).IsValid.Should().Be(valid);
    }
}
