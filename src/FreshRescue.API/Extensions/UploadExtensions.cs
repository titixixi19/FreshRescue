using FreshRescue.Business.Features.Uploads;

namespace FreshRescue.API.Extensions;

/// <summary>T5-TV6-01. Đăng ký service upload ảnh, lưu dưới <c>wwwroot/uploads</c> (OQ-09).</summary>
public static class UploadExtensions
{
    public static IServiceCollection AddUploads(this IServiceCollection services)
    {
        services.AddOptions<UploadOptions>()
            .Configure<IWebHostEnvironment>((o, env) =>
                o.RootPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads"));
        services.AddScoped<IImageUploadService, ImageUploadService>();

        return services;
    }
}
