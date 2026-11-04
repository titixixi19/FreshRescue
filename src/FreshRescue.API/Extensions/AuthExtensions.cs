using System.Text;
using FreshRescue.Business.Common;
using FreshRescue.DataAccess;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

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
        // 1. Identity Core với AppUser, AppRole
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<FreshRescueDbContext>()
        .AddDefaultTokenProviders();

        // 2. JWT Authentication
        var jwtSection = configuration.GetSection("Jwt");
        var keyString = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 32)
        {
            throw new InvalidOperationException(
                "Cấu hình Jwt:Key bị thiếu, rỗng hoặc ngắn hơn 32 ký tự. "
                + "Hãy chép mục 'Jwt' từ appsettings.Development.json.example vào appsettings.Development.json.");
        }
        var issuer = jwtSection["Issuer"] ?? "FreshRescue.API";
        var audience = jwtSection["Audience"] ?? "FreshRescue.Client";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            // Hỗ trợ SignalR truyền access token qua query string (?access_token=...)
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // 3. Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireBrandOwner", policy =>
                policy.RequireRole(AppRoles.BrandManager)
                      .RequireClaim("brand_id"));

            options.AddPolicy("RequireStoreStaff", policy =>
                policy.RequireRole(AppRoles.StoreStaff)
                      .RequireClaim("store_id"));

            options.AddPolicy("StaffOrBrandManager", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole(AppRoles.StoreStaff) || ctx.User.IsInRole(AppRoles.BrandManager)));
        });

        // 4. CurrentUser & HttpContextAccessor
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
