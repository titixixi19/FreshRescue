using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FreshRescue.API.Extensions;

/// <summary>Hai endpoint OData tự sinh không có XML comment để Swagger đọc — gắn mô tả tay (NFR-07).</summary>
public sealed class ODataMetadataDescriptionFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> _summaries = new()
    {
        ["/odata"] = "Service document OData: liệt kê các entity set công khai (Stores, Offers) — Ch.04.",
        ["/odata/$metadata"] = "Metadata CSDL của OData: kiểu, thuộc tính và quan hệ để client dựng truy vấn — Ch.04.",
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var (path, summary) in _summaries)
        {
            if (swaggerDoc.Paths.TryGetValue(path, out var item))
            {
                foreach (var operation in item.Operations.Values.Where(op => string.IsNullOrEmpty(op.Summary)))
                {
                    operation.Summary = summary;
                }
            }
        }
    }
}

/// <summary>Chủ sở hữu: Hiếu (T2-HIEU-02). Thêm nút Authorize (Bearer) khi làm JWT.</summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FreshRescue API",
                Version = "v1",
                Description = "Nền tảng giải cứu thực phẩm & bán túi mù F&B — PRN232",
            });

            // Swagger đọc XML comment của controller (NFR-07).
            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
            if (File.Exists(xmlFile))
            {
                o.IncludeXmlComments(xmlFile);
            }

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Nhập token JWT theo định dạng: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            };

            o.AddSecurityDefinition("Bearer", securityScheme);
            o.DocumentFilter<ODataMetadataDescriptionFilter>();

            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
        });

        return services;
    }
}
