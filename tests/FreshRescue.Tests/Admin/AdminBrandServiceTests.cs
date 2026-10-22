using AutoMapper;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Admin;
using FreshRescue.Business.Features.Admin.Dtos;
using FreshRescue.Business.Mapping;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Admin;

public class AdminBrandServiceTests
{
    private static readonly DateTime _now = new(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc);

    private readonly FreshRescueDbContext _context;
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Brand _brand;

    public AdminBrandServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FreshRescueDbContext(options, new FixedTimeProvider(_now));

        var manager = new AppUser
        {
            FullName = "Quản lý thương hiệu",
            UserName = "manager@example.test",
            Email = "manager@example.test"
        };
        _brand = new Brand
        {
            Name = "Thương hiệu thử nghiệm",
            Slug = "thuong-hieu-thu-nghiem",
            OwnerUserId = manager.Id,
            Owner = manager,
            Status = BrandStatus.PendingApproval
        };
        _context.Brands.Add(_brand);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ApproveAsync_DangChoDuyet_ChuyenApprovedVaLuuNguoiDuyetAsync()
    {
        var result = await CreateService().ApproveAsync(
            _brand.Id, CancellationToken.None);

        result.Status.Should().Be(BrandStatus.Approved);
        result.ApprovedAt.Should().Be(_now);
        result.ApprovedByUserId.Should().Be(_adminId);
        result.OwnerEmail.Should().Be("manager@example.test");

        var saved = await _context.Brands.AsNoTracking()
            .SingleAsync(x => x.Id == _brand.Id);
        saved.Status.Should().Be(BrandStatus.Approved);
        saved.ApprovedByUserId.Should().Be(_adminId);
    }

    [Fact]
    public async Task ApproveAsync_DaApproved_Bao409Async()
    {
        _brand.Status = BrandStatus.Approved;
        await _context.SaveChangesAsync();

        var act = () => CreateService().ApproveAsync(
            _brand.Id, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task RejectAsync_DangChoDuyet_LuuLyDoAsync()
    {
        var request = new RejectBrandRequest { Reason = "  Thiếu giấy tờ đăng ký  " };

        var result = await CreateService().RejectAsync(
            _brand.Id, request, CancellationToken.None);

        result.Status.Should().Be(BrandStatus.Rejected);
        result.StatusReason.Should().Be("Thiếu giấy tờ đăng ký");
        (await _context.Brands.AsNoTracking()
            .SingleAsync(x => x.Id == _brand.Id))
            .Status.Should().Be(BrandStatus.Rejected);
    }

    [Fact]
    public async Task RejectAsync_DaApproved_Bao409Async()
    {
        _brand.Status = BrandStatus.Approved;
        await _context.SaveChangesAsync();

        var act = () => CreateService().RejectAsync(
            _brand.Id,
            new RejectBrandRequest { Reason = "Không đạt yêu cầu" },
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task SuspendAsync_DaApproved_TamKhoaVaGiuLichSuDuyetAsync()
    {
        _brand.Status = BrandStatus.Approved;
        _brand.ApprovedAt = _now.AddDays(-1);
        _brand.ApprovedByUserId = _adminId;
        await _context.SaveChangesAsync();

        var result = await CreateService().SuspendAsync(
            _brand.Id,
            new SuspendBrandRequest { Reason = "  Vi phạm quy định  " },
            CancellationToken.None);

        result.Status.Should().Be(BrandStatus.Suspended);
        result.StatusReason.Should().Be("Vi phạm quy định");
        result.ApprovedAt.Should().Be(_now.AddDays(-1));
        result.ApprovedByUserId.Should().Be(_adminId);
    }

    [Fact]
    public async Task SuspendAsync_ChuaApproved_Bao409Async()
    {
        var act = () => CreateService().SuspendAsync(
            _brand.Id,
            new SuspendBrandRequest { Reason = "Vi phạm quy định" },
            CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task GetAllAsync_LocTheoTrangThai_VaTraPhanTrangAsync()
    {
        var result = await CreateService().GetAllAsync(
            new GetAdminBrandsRequest
            {
                Status = BrandStatus.PendingApproval,
                Page = 1,
                PageSize = 10
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Id.Should().Be(_brand.Id);
    }

    private AdminBrandService CreateService()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.UserId).Returns(_adminId);
        var mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile<BrandMappingProfile>()).CreateMapper();

        return new AdminBrandService(
            new UnitOfWork(_context),
            mapper,
            currentUser.Object,
            new FixedTimeProvider(_now));
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
