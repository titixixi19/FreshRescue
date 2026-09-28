namespace FreshRescue.API.Extensions;

/// <summary>
/// Chủ sở hữu: Hiếu (T3-HIEU-01…03). Identity + JWT + Refresh Token, claim <c>brand_id</c> /
/// <c>store_id</c>, policy <c>RequireBrandOwner</c> / <c>RequireStoreStaff</c>, và cài đặt
/// <c>ICurrentUser</c> (03-API-CONTRACT.md §1.5).
/// </summary>
public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO(Hieu, 2026-09-29): cấu hình JwtBearer theo 03-API-CONTRACT.md §1.5.
        services.AddAuthentication();
        services.AddAuthorization();

        return services;
    }
}
