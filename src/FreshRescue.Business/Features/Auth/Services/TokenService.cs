using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FreshRescue.Entities.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FreshRescue.Business.Features.Auth.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _clock;

    public TokenService(IConfiguration configuration, TimeProvider clock)
    {
        _configuration = configuration;
        _clock = clock;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(
        AppUser user,
        IReadOnlyList<string> roles,
        Guid? brandId,
        Guid? storeId)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var keyString = jwtSection["Key"]
            ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key.");
        var issuer = jwtSection["Issuer"] ?? "FreshRescue.API";
        var audience = jwtSection["Audience"] ?? "FreshRescue.Client";
        var expirationMinutes = int.TryParse(jwtSection["AccessTokenExpirationMinutes"], out var expMin) ? expMin : 15;

        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(expirationMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        if (brandId.HasValue)
        {
            claims.Add(new Claim("brand_id", brandId.Value.ToString()));
        }

        if (storeId.HasValue)
        {
            claims.Add(new Claim("store_id", storeId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = credentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GetRefreshTokenExpiresAt()
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var days = int.TryParse(jwtSection["RefreshTokenExpirationDays"], out var d) ? d : 7;
        return _clock.GetUtcNow().UtcDateTime.AddDays(days);
    }
}
