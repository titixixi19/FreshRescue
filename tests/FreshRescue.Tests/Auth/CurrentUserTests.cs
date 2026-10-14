using System.Security.Claims;
using FluentAssertions;
using FreshRescue.API.Extensions;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Http;

namespace FreshRescue.Tests.Auth;

public class CurrentUserTests
{
    [Fact]
    public void CurrentUser_DocDungThongTinTuClaims()
    {
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, AppRoles.Customer),
            new(ClaimTypes.Role, AppRoles.BrandManager),
            new("brand_id", brandId.ToString()),
            new("store_id", storeId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var contextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUser = new CurrentUser(contextAccessor);

        currentUser.UserId.Should().Be(userId);
        currentUser.BrandId.Should().Be(brandId);
        currentUser.StoreId.Should().Be(storeId);
        currentUser.Roles.Should().Contain([AppRoles.Customer, AppRoles.BrandManager]);
        currentUser.IsInRole(AppRoles.Customer).Should().BeTrue();
        currentUser.IsInRole(AppRoles.SystemAdmin).Should().BeFalse();
    }

    [Fact]
    public void CurrentUser_TraVeNullKhiChuaDangNhap()
    {
        var httpContext = new DefaultHttpContext();
        var contextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUser = new CurrentUser(contextAccessor);

        currentUser.UserId.Should().BeNull();
        currentUser.BrandId.Should().BeNull();
        currentUser.StoreId.Should().BeNull();
        currentUser.Roles.Should().BeEmpty();
        currentUser.IsInRole(AppRoles.Customer).Should().BeFalse();
    }
}
