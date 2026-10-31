using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.Business.Features.Catalog.Validators;
using FreshRescue.Business.Mapping;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Catalog;

public class BrandStaffServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly AppUser _owner;
    private readonly Brand _brand;
    private readonly Store _store;

    public BrandStaffServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));

        _owner = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Chủ thương hiệu",
            UserName = "owner@brand.test",
            Email = "owner@brand.test"
        };

        _brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = "Thương hiệu A",
            Slug = "thuong-hieu-a",
            OwnerUserId = _owner.Id,
            Owner = _owner,
            Status = BrandStatus.Approved
        };

        _store = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = _brand.Id,
            Name = "Chi nhánh Nguyễn Huệ",
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

        _context.AddRange(_owner, _brand, _store);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_TraVeNhanVienThuocBrand_ThanhCong()
    {
        var staffUser = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyễn Văn Nhân Viên",
            Email = "staff@brand.test",
            UserName = "staff@brand.test"
        };
        var staffRecord = new StoreStaff
        {
            Id = Guid.NewGuid(),
            UserId = staffUser.Id,
            StoreId = _store.Id,
            Position = "Thu ngân",
            IsActive = true,
            CreatedAt = _now
        };
        _context.AddRange(staffUser, staffRecord);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var result = await service.GetAllAsync(new GetBrandStaffRequest(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].FullName.Should().Be("Nguyễn Văn Nhân Viên");
        result.Items[0].StoreName.Should().Be("Chi nhánh Nguyễn Huệ");
        result.Items[0].Position.Should().Be("Thu ngân");
        result.Items[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_StoreKhongThuocBrand_QuangForbidden()
    {
        var otherBrandStoreId = SeedOtherBrandStore().Id;
        var service = CreateService();

        var act = () => service.GetAllAsync(new GetBrandStaffRequest { StoreId = otherBrandStoreId }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task CreateAsync_StoreKhongThuocBrand_QuangForbidden()
    {
        var otherBrandStoreId = SeedOtherBrandStore().Id;
        var service = CreateService();

        var request = new CreateBrandStaffRequest
        {
            FullName = "Trần Thị Bình",
            Email = "binh@brand.test",
            Password = "password123",
            StoreId = otherBrandStoreId
        };

        var act = () => service.CreateAsync(request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task CreateAsync_EmailDaTonTai_QuangValidationFailed()
    {
        var existingUser = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Đã tồn tại",
            Email = "duplicate@brand.test",
            UserName = "duplicate@brand.test"
        };

        var userStore = new Mock<IUserStore<AppUser>>();
        var userManager = new Mock<UserManager<AppUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(x => x.FindByEmailAsync("duplicate@brand.test")).ReturnsAsync(existingUser);

        var service = CreateService(userManagerMock: userManager);

        var request = new CreateBrandStaffRequest
        {
            FullName = "Nhân viên mới",
            Email = "duplicate@brand.test",
            Password = "password123",
            StoreId = _store.Id
        };

        var act = () => service.CreateAsync(request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task CreateAsync_HopLe_TaoUserVaStoreStaffThanhCong()
    {
        var userStore = new Mock<IUserStore<AppUser>>();
        var userManager = new Mock<UserManager<AppUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<AppUser, string>((u, _) => u.Id = Guid.NewGuid());
        userManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), AppRoles.StoreStaff))
            .ReturnsAsync(IdentityResult.Success);

        var roleStore = new Mock<IRoleStore<AppRole>>();
        var roleManager = new Mock<RoleManager<AppRole>>(roleStore.Object, null!, null!, null!, null!);
        roleManager.Setup(x => x.RoleExistsAsync(AppRoles.StoreStaff)).ReturnsAsync(true);

        var service = CreateService(userManagerMock: userManager, roleManagerMock: roleManager);

        var request = new CreateBrandStaffRequest
        {
            FullName = "Trần Thị Bình",
            Email = "binh@brand.test",
            Password = "password123",
            PhoneNumber = "0901234567",
            StoreId = _store.Id,
            Position = "Thu ngân"
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Trần Thị Bình");
        result.Email.Should().Be("binh@brand.test");
        result.StoreId.Should().Be(_store.Id);
        result.StoreName.Should().Be("Chi nhánh Nguyễn Huệ");
        result.Position.Should().Be("Thu ngân");
        result.IsActive.Should().BeTrue();

        var dbRecord = await _context.StoreStaff.FirstOrDefaultAsync(s => s.Id == result.Id);
        dbRecord.Should().NotBeNull();
        dbRecord!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_NhanVienKhongTonTai_QuangNotFound()
    {
        var service = CreateService();

        var act = () => service.RemoveAsync(Guid.NewGuid(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task RemoveAsync_NhanVienThuocBrandKhac_QuangForbidden()
    {
        var otherStore = SeedOtherBrandStore();
        var otherUser = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Khác",
            Email = "other@test.vn",
            UserName = "other@test.vn"
        };
        var otherStaff = new StoreStaff
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            StoreId = otherStore.Id,
            IsActive = true
        };
        _context.AddRange(otherUser, otherStaff);
        await _context.SaveChangesAsync();

        var service = CreateService();

        var act = () => service.RemoveAsync(otherStaff.Id, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ForbiddenScope);
    }

    [Fact]
    public async Task RemoveAsync_HopLe_DatIsActiveFalse()
    {
        var staffUser = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Staff",
            Email = "staff@test.vn",
            UserName = "staff@test.vn"
        };
        var staffRecord = new StoreStaff
        {
            Id = Guid.NewGuid(),
            UserId = staffUser.Id,
            StoreId = _store.Id,
            IsActive = true
        };
        _context.AddRange(staffUser, staffRecord);
        await _context.SaveChangesAsync();

        var service = CreateService();

        await service.RemoveAsync(staffRecord.Id, CancellationToken.None);

        var updated = await _context.StoreStaff.FindAsync(staffRecord.Id);
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Validator_CreateBrandStaffRequestValidator_ValidateDung()
    {
        var validator = new CreateBrandStaffRequestValidator();

        var valid = new CreateBrandStaffRequest
        {
            FullName = "Trần Thị Bình",
            Email = "binh@example.com",
            Password = "matkhau123",
            StoreId = Guid.NewGuid()
        };
        validator.Validate(valid).IsValid.Should().BeTrue();

        var invalid = new CreateBrandStaffRequest
        {
            FullName = "",
            Email = "not-an-email",
            Password = "123",
            StoreId = Guid.Empty
        };
        var result = validator.Validate(invalid);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }

    private BrandStaffService CreateService(
        Mock<UserManager<AppUser>>? userManagerMock = null,
        Mock<RoleManager<AppRole>>? roleManagerMock = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.UserId).Returns(_owner.Id);
        currentUser.Setup(x => x.BrandId).Returns(_brand.Id);

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<StoreMappingProfile>();
            cfg.AddProfile<BrandStaffMappingProfile>();
        }).CreateMapper();

        if (userManagerMock == null)
        {
            var userStore = new Mock<IUserStore<AppUser>>();
            userManagerMock = new Mock<UserManager<AppUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        if (roleManagerMock == null)
        {
            var roleStore = new Mock<IRoleStore<AppRole>>();
            roleManagerMock = new Mock<RoleManager<AppRole>>(roleStore.Object, null!, null!, null!, null!);
        }

        return new BrandStaffService(
            new UnitOfWork(_context),
            userManagerMock.Object,
            roleManagerMock.Object,
            currentUser.Object,
            mapper);
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
        var otherStore = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = otherBrand.Id,
            Name = "Chi nhánh khác",
            AddressLine = "123 Khác",
            Ward = "Khác",
            District = "Khác",
            City = "TP.HCM",
            Phone = "0901112222",
            Latitude = 10.7m,
            Longitude = 106.7m,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(22, 0)
        };
        _context.AddRange(otherBrand, otherStore);
        _context.SaveChanges();
        return otherStore;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
