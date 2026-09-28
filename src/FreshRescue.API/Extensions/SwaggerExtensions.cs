namespace FreshRescue.API.Extensions;

/// <summary>Chủ sở hữu: Hiếu (T2-HIEU-02). Thêm nút Authorize (Bearer) khi làm JWT.</summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            // Swagger đọc XML comment của controller (NFR-07).
            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
            if (File.Exists(xmlFile))
            {
                o.IncludeXmlComments(xmlFile);
            }
        });

        return services;
    }
}
