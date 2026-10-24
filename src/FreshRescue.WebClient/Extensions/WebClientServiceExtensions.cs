using FreshRescue.WebClient.Services;
using FreshRescue.WebClient.Services.Mock;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FreshRescue.WebClient.Extensions;

/// <summary>Chủ sở hữu: Đức Anh (T3-TV4-01). Hạ tầng dùng chung cho Areas/Customer và Areas/Dashboard.</summary>
public static class WebClientServiceExtensions
{
    public const string UseMockApiKey = "UseMockApi";

    /// <summary>
    /// <c>"UseMockApi": true</c> → <see cref="MockApiClient"/>, ngược lại → <see cref="HttpApiClient"/>
    /// gọi <c>Api:BaseUrl</c> (07-WORK-BREAKDOWN.md §12.3).
    /// </summary>
    public static IServiceCollection AddApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();
        services.AddSingleton(options);
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ApiSession>();

        if (configuration.GetValue<bool>(UseMockApiKey))
        {
            services.AddSingleton<MockApiStore>();
            services.AddScoped<IApiClient, MockApiClient>();
        }
        else
        {
            services.AddSingleton<TokenRefreshCoordinator>();
            services.AddHttpClient<IApiClient, HttpApiClient>(client =>
            {
                client.BaseAddress = options.ApiRoot;
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        return services;
    }

    /// <summary>
    /// Đăng nhập bằng cookie; cookie mang JWT của API (<see cref="ApiSession"/>). Sống bằng refresh
    /// token — 7 ngày (03-API-CONTRACT.md §1.5).
    /// </summary>
    public static IServiceCollection AddCookieAuth(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = "FreshRescue.Auth";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.ExpireTimeSpan = TimeSpan.FromDays(7);
                o.SlidingExpiration = false;
                o.LoginPath = "/Account/Login";
                o.LogoutPath = "/Account/Logout";
                o.AccessDeniedPath = "/Account/AccessDenied";

                // fetch không đi theo được redirect sang trang HTML — trả mã để JS tự chuyển trang.
                o.Events.OnRedirectToLogin = context => RedirectOrStatus(context, StatusCodes.Status401Unauthorized);
                o.Events.OnRedirectToAccessDenied = context => RedirectOrStatus(context, StatusCodes.Status403Forbidden);
            });

        services.AddAuthorization();
        return services;
    }

    private static Task RedirectOrStatus(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context, int status)
    {
        if (context.Request.IsAjax())
        {
            context.Response.StatusCode = status;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
