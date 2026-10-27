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

public class StoreServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly AppUser _owner;
    private readonly Brand _brand;

    public StoreServiceTests()
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
            Name = "Thực phẩm xanh",
            Slug = "thuc-pham-xanh",
            OwnerUserId = _owner.Id,
            Owner = _owner,
            Status = BrandStatus.Approved
        };

        _context.Brands.Add(_brand);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ChiTraChiNhanhThuocBrandDangNhapAsync()
    {
        var ownStore = NewStore(_brand.Id, "Chi nhánh của tôi");
        var otherStore = SeedOtherBrandStore();
        _context.Stores.Add(ownStore);
        await _context.SaveChangesAsync();

        var result = await CreateService().GetAllAsync(
            new GetBrandStoresRequest(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Id.Should().Be(ownStore.Id);
        result.Items.Should().NotContain(x => x.Id == otherStore.Id);
    }

    [Fact]
    public async Task CreateAsync_HopLe_TaoStoreDungThuongHieuAsync()
    {
        var request = new CreateStoreRequest
        {
            Name = " Chi nhánh mới ",
            AddressLine = "12 Nguyễn Huệ",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "TP.HCM",
            Latitude = 10.775m,
            Longitude = 106.703m,
            Phone = "0901234567",
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(22, 0)
        };

        var result = await CreateService().CreateAsync(request, CancellationToken.None);

        result.BrandId.Should().Be(_brand.Id);
        result.Name.Should().Be("Chi nhánh mới");
        result.Status.Should().Be(StoreStatus.Active);

        var stored = await _context.Stores.AsNoTracking()
            .SingleAsync(x => x.Id == result.Id);
        stored.BrandId.Should().Be(_brand.Id);
        stored.RatingCount.Should().Be(0);
        stored.RatingAverage.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ChiNhanhThuocBrandKhac_Bao403Async()
    {
        var otherStore = SeedOtherBrandStore();
        var request = ValidUpdateRequest();

        var act = () => CreateService().UpdateAsync(
            otherStore.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope && e.StatusCode == 403);
    }

    [Fact]
    public async Task DeleteAsync_ChiNhanhThuocBrandKhac_Bao403Async()
    {
        var otherStore = SeedOtherBrandStore();

        var act = () => CreateService().DeleteAsync(
            otherStore.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.ErrorCode == ErrorCodes.ForbiddenScope && e.StatusCode == 403);

        (await _context.Stores.AsNoTracking()
            .SingleAsync(x => x.Id == otherStore.Id)).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ThanhCong_KhongSuaDiemDanhGiaAsync()
    {
        var store = NewStore(_brand.Id, "Tên cũ");
        store.RatingAverage = 4.5m;
        store.RatingCount = 12;
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var result = await CreateService().UpdateAsync(
            store.Id, ValidUpdateRequest(), CancellationToken.None);

        result.Name.Should().Be("Tên mới");
        result.Status.Should().Be(StoreStatus.Inactive);

        var saved = await _context.Stores.AsNoTracking()
            .SingleAsync(x => x.Id == store.Id);
        saved.RatingAverage.Should().Be(4.5m);
        saved.RatingCount.Should().Be(12);
        saved.BrandId.Should().Be(_brand.Id);
    }

    [Fact]
    public async Task DeleteAsync_ThanhCong_XoaMemStoreAsync()
    {
        var store = NewStore(_brand.Id, "Chi nhánh cần xóa");
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        await CreateService().DeleteAsync(store.Id, CancellationToken.None);

        (await _context.Stores.AnyAsync(x => x.Id == store.Id)).Should().BeFalse();
        var deletedStore = await _context.Stores.IgnoreQueryFilters()
            .AsNoTracking().SingleAsync(x => x.Id == store.Id);
        deletedStore.IsDeleted.Should().BeTrue();
        deletedStore.DeletedAt.Should().Be(_now);
    }

    private StoreService CreateService()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.UserId).Returns(_owner.Id);
        currentUser.Setup(x => x.BrandId).Returns(_brand.Id);

        var mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile<StoreMappingProfile>()).CreateMapper();

        return new StoreService(
            new UnitOfWork(_context), mapper, currentUser.Object);
    }

    private Store SeedOtherBrandStore()
    {
        var otherOwner = new AppUser
        {
            FullName = "Quản lý khác",
            UserName = "other-manager@example.test",
            Email = "other-manager@example.test"
        };
        var otherBrand = new Brand
        {
            Name = "Thương hiệu khác",
            Slug = "thuong-hieu-khac",
            OwnerUserId = otherOwner.Id,
            Owner = otherOwner,
            Status = BrandStatus.Approved
        };
        var otherStore = NewStore(otherBrand.Id, "Chi nhánh người khác");
        _context.AddRange(otherBrand, otherStore);
        _context.SaveChanges();
        return otherStore;
    }

    private static Store NewStore(Guid brandId, string name) => new()
    {
        BrandId = brandId,
        Name = name,
        AddressLine = "12 Nguyễn Huệ",
        Ward = "Bến Nghé",
        District = "Quận 1",
        City = "TP.HCM",
        Phone = "0901234567",
        Latitude = 10.775m,
        Longitude = 106.703m,
        OpenTime = new TimeOnly(8, 0),
        CloseTime = new TimeOnly(22, 0)
    };

    private static UpdateStoreRequest ValidUpdateRequest() => new()
    {
        Name = "Tên mới",
        AddressLine = "14 Nguyễn Huệ",
        Ward = "Bến Nghé",
        District = "Quận 1",
        City = "TP.HCM",
        Phone = "0901234567",
        Latitude = 10.775m,
        Longitude = 106.703m,
        OpenTime = new TimeOnly(7, 0),
        CloseTime = new TimeOnly(23, 0),
        Status = StoreStatus.Inactive
    };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
