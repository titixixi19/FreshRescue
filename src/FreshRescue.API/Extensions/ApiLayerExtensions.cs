using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using FreshRescue.Business.Common;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Extensions;

/// <summary>
/// Điểm vào của tầng API. Chủ sở hữu: Tùng. File này chỉ GỌI các extension của từng người —
/// ai phụ trách phần nào thì sửa file của phần đó, không sửa ở đây.
/// </summary>
public static class ApiLayerExtensions
{
    public const string WebClientCorsPolicy = "WebClient";

    public static IServiceCollection AddApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers()
            // Enum ra JSON là chuỗi — trả số là vỡ Web Client khi thêm giá trị enum (03-API-CONTRACT.md §1).
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .AddODataConfig()
            // Đứng sau formatter XmlSerializer của AddODataConfig: chỉ nhận những kiểu XmlSerializer bó tay.
            .AddMvcOptions(o => o.OutputFormatters.Add(new JsonXmlOutputFormatter()))
            .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = BuildValidationProblem);

        services.AddFluentValidationAutoValidation();

        // Trang AJAX và SignalR JS của Web Client chạy trên origin khác API (Ch.07).
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(WebClientCorsPolicy, p => p
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        services
            .AddSwaggerDocs()
            .AddJwtAuth(configuration)
            .AddRealtime()
            .AddBackgroundJobs()
            .AddQrServiceClient(configuration)
            .AddUploads()
            .AddPasswordResetEmail();

        return services;
    }

    // Lỗi định dạng dữ liệu vào cũng phải theo khuôn ProblemDetails + errorCode (03-API-CONTRACT.md §1.3).
    private static IActionResult BuildValidationProblem(ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToDictionary(
                e => ToCamelCasePath(e.Key),
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Title = "Dữ liệu không hợp lệ",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path,
        };
        problem.Extensions["errorCode"] = ErrorCodes.ValidationFailed;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem);
    }

    // Contract dùng tên trường camelCase ("items[0].quantity"), ModelState trả PascalCase.
    private static string ToCamelCasePath(string key)
        => string.Join('.', key.Split('.').Select(part =>
            part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
}
