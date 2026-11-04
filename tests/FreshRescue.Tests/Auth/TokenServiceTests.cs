using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FreshRescue.Business.Features.Auth.Services;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Identity;
using Microsoft.Extensions.Configuration;

namespace FreshRescue.Tests.Auth;

public class TokenServiceTests
{
    private static readonly DateTime _fixedNow = new(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc);

    private static IConfiguration CreateConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "Test_Secret_Key_At_Least_32_Characters_Long_For_HmacSha256!",
            ["Jwt:Issuer"] = "FreshRescue.API.Test",
            ["Jwt:Audience"] = "FreshRescue.Client.Test",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void GenerateAccessToken_SinhTokenHopLe_CoDayDuCustomClaims()
    {
        var config = CreateConfiguration();
        var clock = new FixedTimeProvider(_fixedNow);
        var tokenService = new TokenService(config, clock);

        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            FullName = "Nguyễn Văn A",
            Email = "nguyenvana@example.com",
        };
        var roles = new[] { AppRoles.Customer, AppRoles.BrandManager, AppRoles.StoreStaff };

        var (token, expiresAt) = tokenService.GenerateAccessToken(user, roles, brandId, storeId);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().Be(_fixedNow.AddMinutes(15));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("FreshRescue.API.Test");
        jwt.Audiences.Should().Contain("FreshRescue.Client.Test");

        var claims = jwt.Claims.ToList();
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "nguyenvana@example.com");
        claims.Count(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == ClaimTypes.Email).Should().Be(1);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == "Nguyễn Văn A");

        foreach (var role in roles)
        {
            claims.Count(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == role).Should().Be(1);
        }

        claims.Should().Contain(c => c.Type == "brand_id" && c.Value == brandId.ToString());
        claims.Should().Contain(c => c.Type == "store_id" && c.Value == storeId.ToString());
    }

    [Fact]
    public void GenerateRefreshToken_TraVeChuoiBase64HopLe()
    {
        var config = CreateConfiguration();
        var clock = new FixedTimeProvider(_fixedNow);
        var tokenService = new TokenService(config, clock);

        var refreshToken = tokenService.GenerateRefreshToken();

        refreshToken.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GetRefreshTokenExpiresAt_MacDinh7NgaySau()
    {
        var config = CreateConfiguration();
        var clock = new FixedTimeProvider(_fixedNow);
        var tokenService = new TokenService(config, clock);

        var expiresAt = tokenService.GetRefreshTokenExpiresAt();

        expiresAt.Should().Be(_fixedNow.AddDays(7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short_key_under_32_chars")]
    public void GenerateAccessToken_NemLoiKhiKeyRongHoacNhoHon32KyTu(string invalidKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = invalidKey,
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var clock = new FixedTimeProvider(_fixedNow);
        var tokenService = new TokenService(config, clock);

        var action = () => tokenService.GenerateAccessToken(new AppUser { Id = Guid.NewGuid() }, [], null, null);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 ký tự*");
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
