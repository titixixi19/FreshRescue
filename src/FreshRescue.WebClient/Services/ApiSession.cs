using System.Globalization;
using System.Security.Claims;
using FreshRescue.WebClient.Models.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FreshRescue.WebClient.Services;

/// <summary>Cặp token đang dùng để gọi API.</summary>
public record ApiTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);

/// <summary>
/// Phiên đăng nhập của WebClient: một cookie mã hoá, HttpOnly, chứa claim cho <c>[Authorize]</c>
/// kèm access token + refresh token để gọi API. Token không bao giờ lộ ra JavaScript.
/// </summary>
/// <remarks>
/// Claim chép từ <c>user</c> của <c>/auth/login</c> (03-API-CONTRACT.md §1.5), nên
/// <c>[Authorize(Roles = AppRoles.StoreStaff)]</c> và <c>User.FindFirst("store_id")</c> dùng được
/// ngay ở cả Areas/Customer lẫn Areas/Dashboard.
/// </remarks>
public class ApiSession
{
    public const string BrandIdClaim = "brand_id";
    public const string StoreIdClaim = "store_id";

    private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    private const string AccessTokenName = "access_token";
    private const string RefreshTokenName = "refresh_token";
    private const string ExpiresAtName = "expires_at";

    private readonly IHttpContextAccessor _httpContextAccessor;

    // Cookie mới chỉ tới trình duyệt cùng response, nên sau khi đổi token giữa request thì các lần
    // gọi API tiếp theo trong CÙNG request phải đọc token ở đây, không đọc lại cookie cũ.
    private ApiTokens? _current;
    private bool _signedOut;

    public ApiSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("ApiSession chỉ dùng được bên trong một HTTP request.");

    /// <summary>Ghi cookie sau khi đăng nhập, đăng ký, hoặc đổi refresh token.</summary>
    public async Task SignInAsync(AuthResponse auth)
    {
        var user = auth.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (user.BrandId is { } brandId)
        {
            claims.Add(new Claim(BrandIdClaim, brandId.ToString()));
        }

        if (user.StoreId is { } storeId)
        {
            claims.Add(new Claim(StoreIdClaim, storeId.ToString()));
        }

        var properties = new AuthenticationProperties { IsPersistent = true };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = AccessTokenName, Value = auth.AccessToken },
            new AuthenticationToken { Name = RefreshTokenName, Value = auth.RefreshToken },
            new AuthenticationToken
            {
                Name = ExpiresAtName,
                Value = auth.ExpiresAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            },
        ]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
        await HttpContext.SignInAsync(Scheme, principal, properties);

        _current = new ApiTokens(auth.AccessToken, auth.RefreshToken, auth.ExpiresAt.ToUniversalTime());
        _signedOut = false;
    }

    public async Task SignOutAsync()
    {
        await HttpContext.SignOutAsync(Scheme);
        _current = null;
        _signedOut = true;
    }

    /// <summary>Null khi chưa đăng nhập.</summary>
    public async Task<ApiTokens?> GetTokensAsync()
    {
        if (_signedOut)
        {
            return null;
        }

        if (_current is not null)
        {
            return _current;
        }

        var result = await HttpContext.AuthenticateAsync(Scheme);
        if (!result.Succeeded || result.Properties is null)
        {
            return null;
        }

        var accessToken = result.Properties.GetTokenValue(AccessTokenName);
        var refreshToken = result.Properties.GetTokenValue(RefreshTokenName);
        var expiresAt = result.Properties.GetTokenValue(ExpiresAtName);
        if (accessToken is null || refreshToken is null)
        {
            return null;
        }

        _current = new ApiTokens(
            accessToken,
            refreshToken,
            DateTime.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed.ToUniversalTime()
                : DateTime.MinValue);

        return _current;
    }
}
