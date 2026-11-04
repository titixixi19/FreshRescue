using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Business.Mapping;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Catalog;

public class BrandProfileServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly AppUser _owner;
    private readonly Brand _brand;

    public BrandProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));

        _owner = new AppUser
        {
            FullName = "Quản lý thương hiệu",
            UserName = "manager@example.test",
            Email = "manager@example.test"
        };
        _brand = new Brand
        {
            Name = "Thương hiệu A",
            Slug = "thuong-hieu-a",
            OwnerUserId = _owner.Id,
            Owner = _owner,
            Status = BrandStatus.Approved
        };
        _context.Brands.Add(_brand);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProfileAsync_DungChuSoHuu_TraHoSoThuongHieuAsync()
    {
        var result = await CreateService(_brand.Id, _owner.Id)
            .GetProfileAsync(CancellationToken.None);

        result.Id.Should().Be(_brand.Id);
        result.Name.Should().Be("Thương hiệu A");
        result.Slug.Should().Be("thuong-hieu-a");
    }

    [Fact]
    public async Task GetProfileAsync_KhongCoBrandScope_Bao403Async()
    {
        var act = () => CreateService(null, _owner.Id)
            .GetProfileAsync(CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 403 && e.ErrorCode == ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task GetProfileAsync_KhongPhaiChuSoHuu_Bao404Async()
    {
        var act = () => CreateService(_brand.Id, Guid.NewGuid())
            .GetProfileAsync(CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 404 && e.ErrorCode == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateProfileAsync_HopLe_ChuanHoaSlugVaLuuThayDoiAsync()
    {
        var request = new UpdateBrandProfileRequest
        {
            Name = "  Thương hiệu mới  ",
            Slug = "  THUONG-HIEU-MOI  ",
            LogoUrl = "/uploads/brands/new.png",
            Description = "Thông tin mới"
        };

        var result = await CreateService(_brand.Id, _owner.Id)
            .UpdateProfileAsync(request, CancellationToken.None);

        result.Name.Should().Be("Thương hiệu mới");
        result.Slug.Should().Be("thuong-hieu-moi");

        var saved = await _context.Brands.AsNoTracking()
            .SingleAsync(x => x.Id == _brand.Id);
        saved.Name.Should().Be("Thương hiệu mới");
        saved.OwnerUserId.Should().Be(_owner.Id);
        saved.Status.Should().Be(BrandStatus.Approved);
    }

    [Fact]
    public async Task UpdateProfileAsync_SlugTrung_Bao400VaKhongSuaBrandAsync()
    {
        SeedOtherBrand(slug: "slug-da-ton-tai");

        var request = new UpdateBrandProfileRequest
        {
            Name = "Tên mới",
            Slug = "SLUG-DA-TON-TAI"
        };
        var act = () => CreateService(_brand.Id, _owner.Id)
            .UpdateProfileAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ValidationFailed);

        (await _context.Brands.AsNoTracking()
            .SingleAsync(x => x.Id == _brand.Id))
            .Slug.Should().Be("thuong-hieu-a");
    }

    [Fact]
    public async Task UpdateProfileAsync_SlugCuaBrandDaXoaMem_VanBao400Async()
    {
        SeedOtherBrand(slug: "slug-da-xoa", isDeleted: true);

        var request = new UpdateBrandProfileRequest
        {
            Name = "Tên mới",
            Slug = "slug-da-xoa"
        };
        var act = () => CreateService(_brand.Id, _owner.Id)
            .UpdateProfileAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 400 && e.ErrorCode == ErrorCodes.ValidationFailed);
    }

    private BrandProfileService CreateService(Guid? brandId, Guid? userId)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.BrandId).Returns(brandId);
        currentUser.Setup(x => x.UserId).Returns(userId);
        var mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile<BrandMappingProfile>()).CreateMapper();

        return new BrandProfileService(
            new UnitOfWork(_context), mapper, currentUser.Object);
    }

    private void SeedOtherBrand(string slug, bool isDeleted = false)
    {
        var otherOwner = new AppUser
        {
            FullName = "Quản lý khác",
            UserName = "other-manager@example.test",
            Email = "other-manager@example.test"
        };
        var otherBrand = new Brand
        {
            Name = "Thương hiệu B",
            Slug = slug,
            OwnerUserId = otherOwner.Id,
            Owner = otherOwner,
            IsDeleted = isDeleted
        };
        _context.Brands.Add(otherBrand);
        _context.SaveChanges();
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
