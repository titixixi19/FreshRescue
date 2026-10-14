using FreshRescue.Business.Common;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Middleware;

/// <summary>
/// Dịch <see cref="AppException"/> sang ProblemDetails đúng khuôn 03-API-CONTRACT.md §1.3.
/// Đây là lý do controller không cần <c>try/catch</c>.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogInformation("Lỗi nghiệp vụ {ErrorCode} tại {Path}: {Detail}", ex.ErrorCode, context.Request.Path, ex.Message);
            await WriteProblemAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client đã huỷ request — không còn ai nhận phản hồi, không phải lỗi hệ thống.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi tại {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, null, "Đã có lỗi xảy ra phía máy chủ.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string? errorCode, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Type = errorCode is null ? null : $"https://freshrescue.dev/errors/{ToKebab(errorCode)}",
            Title = TitleFor(status),
            Status = status,
            Detail = detail,
            Instance = context.Request.Path,
        };

        // Client bắt lỗi theo errorCode, không theo detail (03-API-CONTRACT.md §1.3).
        if (errorCode is not null)
        {
            problem.Extensions["errorCode"] = errorCode;
        }

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", context.RequestAborted);
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Yêu cầu không hợp lệ",
        StatusCodes.Status401Unauthorized => "Chưa xác thực",
        StatusCodes.Status403Forbidden => "Không có quyền thực hiện",
        StatusCodes.Status404NotFound => "Không tìm thấy",
        StatusCodes.Status409Conflict => "Xung đột dữ liệu",
        _ => "Lỗi hệ thống",
    };

    // OFFER_SOLD_OUT → offer-sold-out, khớp ví dụ "type" trong contract.
    private static string ToKebab(string errorCode) => errorCode.ToLowerInvariant().Replace('_', '-');
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
