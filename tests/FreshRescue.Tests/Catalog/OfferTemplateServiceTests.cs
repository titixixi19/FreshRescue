using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Catalog;

public class OfferTemplateServiceTests : IDisposable
{
    private static readonly DateTimeOffset _now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private readonly FreshRescueDbContext _context;
    private readonly AppUser _owner;
    private readonly Brand _brand;
    private readonly Category _category;

    public OfferTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));
        _owner = new AppUser
        {
            FullName = "Brand Manager",
            UserName = "owner@example.test",
            Email = "owner@example.test"
        };
        _brand = new Brand
        {
            Name = "Brand A",
            Slug = "brand-a",
            OwnerUserId = _owner.Id,
            Owner = _owner,
            Status = BrandStatus.Approved
        };
        _category = new Category { Name = "Bánh mặn", Slug = "banh-man" };
        _context.Brands.Add(_brand);
        _context.Categories.Add(_category);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidSurpriseBag_CreatesTemplateForCurrentBrandAsync()
    {
        var result = await CreateService().CreateAsync(ValidRequest(), CancellationToken.None);

        result.BrandId.Should().Be(_brand.Id);
        result.Name.Should().Be("Túi bánh ngon");
        (await _context.OfferTemplates.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(OfferType.SurpriseBag, 64, 75)]
    [InlineData(OfferType.SurpriseBag, 65, 76)]
    [InlineData(OfferType.ClearanceItem, 35, 50)]
    [InlineData(OfferType.ClearanceItem, 40, 60)]
    public async Task CreateAsync_BrandRangeOutsideFloor_ReturnsPriceOutOfBandAsync(
        OfferType type, decimal min, decimal max)
    {
        var request = ValidRequest();
        request.OfferType = type;
        request.BrandMinDiscountPercent = min;
        request.BrandMaxDiscountPercent = max;

        var action = () => CreateService().CreateAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<AppException>()
            .Where(x => x.ErrorCode == ErrorCodes.PriceOutOfBand && x.StatusCode == 400);
    }

    [Fact]
    public async Task CreateAsync_SuggestedPriceOutsideBrandRange_ReturnsPriceOutOfBandAsync()
    {
        var request = ValidRequest();
        request.OriginalPrice = 100_000m;
        request.SuggestedPrice = 35_000m; // giảm 65%: hợp lệ theo sàn nhưng ngoài khoảng brand 70–75%.
        request.BrandMinDiscountPercent = 70m;

        var action = () => CreateService().CreateAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<AppException>()
            .Where(x => x.ErrorCode == ErrorCodes.PriceOutOfBand && x.StatusCode == 400);
    }

    [Fact]
    public async Task CreateAsync_MissingCategory_ReturnsValidationFailedAsync()
    {
        var request = ValidRequest();
        request.CategoryId = Guid.NewGuid();

        var action = () => CreateService().CreateAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<AppException>()
            .Where(x => x.ErrorCode == ErrorCodes.ValidationFailed && x.StatusCode == 400);
    }

    [Fact]
    public async Task UpdateAsync_OtherBrandTemplate_ReturnsForbiddenAsync()
    {
        var other = SeedOtherBrandTemplate();

        var action = () => CreateService().UpdateAsync(other.Id, ValidRequest(), CancellationToken.None);
        await action.Should().ThrowAsync<AppException>()
            .Where(x => x.ErrorCode == ErrorCodes.ForbiddenScope && x.StatusCode == 403);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTemplatesFromCurrentBrandAsync()
    {
        await CreateService().CreateAsync(ValidRequest(), CancellationToken.None);
        SeedOtherBrandTemplate();

        var result = await CreateService().GetAllAsync(
            new GetOfferTemplatesRequest(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(x => x.BrandId == _brand.Id);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTemplate_SoftDeletesAsync()
    {
        var created = await CreateService().CreateAsync(ValidRequest(), CancellationToken.None);
        await CreateService().DeleteAsync(created.Id, CancellationToken.None);

        (await _context.OfferTemplates.CountAsync()).Should().Be(0);
        var deleted = await _context.OfferTemplates.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == created.Id);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().Be(_now.UtcDateTime);
    }

    [Fact]
    public async Task GetAllAsync_NoBrandScope_ReturnsForbiddenAsync()
    {
        var action = () => CreateService(brandId: null, useOwnBrand: false).GetAllAsync(
            new GetOfferTemplatesRequest(), CancellationToken.None);
        await action.Should().ThrowAsync<AppException>()
            .Where(x => x.ErrorCode == ErrorCodes.ForbiddenScope && x.StatusCode == 403);
    }

    private IOfferTemplateService CreateService(
    Guid? brandId = null,
    bool useOwnBrand = true)
    {
        var currentUser = new Mock<ICurrentUser>();

        currentUser.Setup(x => x.UserId).Returns(_owner.Id);

        currentUser.Setup(x => x.BrandId)
            .Returns(useOwnBrand ? brandId ?? _brand.Id : brandId);

        // Giả lập tài khoản BrandManager
        currentUser.Setup(x => x.IsInRole(
            FreshRescue.Entities.Constants.AppRoles.BrandManager))
            .Returns(true);

        return new OfferTemplateService(
            new UnitOfWork(_context),
            currentUser.Object);
    }

    private UpsertOfferTemplateRequest ValidRequest() => new()
    {
        Name = " Túi bánh ngon ",
        CategoryId = _category.Id,
        OfferType = OfferType.SurpriseBag,
        OriginalPrice = 100_000m,
        SuggestedPrice = 30_000m,
        BrandMinDiscountPercent = 65m,
        BrandMaxDiscountPercent = 75m
    };

    private OfferTemplate SeedOtherBrandTemplate()
    {
        var otherOwner = new AppUser
        {
            FullName = "Other Manager",
            UserName = "other@example.test",
            Email = "other@example.test"
        };
        var otherBrand = new Brand
        {
            Name = "Brand B",
            Slug = "brand-b",
            OwnerUserId = otherOwner.Id,
            Owner = otherOwner,
            Status = BrandStatus.Approved
        };
        var template = new OfferTemplate
        {
            BrandId = otherBrand.Id,
            Brand = otherBrand,
            CategoryId = _category.Id,
            Name = "Mẫu của brand khác",
            OfferType = OfferType.SurpriseBag,
            OriginalPrice = 100_000m,
            SuggestedPrice = 30_000m,
            BrandMinDiscountPercent = 65m,
            BrandMaxDiscountPercent = 75m
        };
        _context.OfferTemplates.Add(template);
        _context.SaveChanges();
        return template;
    }

    public void Dispose() => _context.Dispose();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
