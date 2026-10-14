using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FreshRescue.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Mỗi người thêm dòng đăng ký service của lát mình vào đúng nhóm dưới đây.
        // Giữ thứ tự nhóm cố định để hai PR cùng sửa file này ít xung đột.

        // --- Auth (Hiếu) ---
        services.AddScoped<Features.Auth.Services.ITokenService, Features.Auth.Services.TokenService>();
        services.AddScoped<Features.Auth.Services.IAuthService, Features.Auth.Services.AuthService>();

        // --- Catalog + Admin (Đình Đức) ---

        // --- Sales (Quang Huy) ---

        // --- Ordering + Redemption (Tùng) ---

        return services;
    }
}
