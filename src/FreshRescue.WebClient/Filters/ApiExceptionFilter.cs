using FreshRescue.WebClient.Extensions;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace FreshRescue.WebClient.Filters;

/// <summary>
/// Lưới cuối cho <see cref="ApiException"/> mà controller không tự bắt — controller chỉ cần bắt
/// những lỗi muốn hiện ngay trên form.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>401 → đăng xuất rồi về trang đăng nhập (refresh token cũng đã hết hạn).</item>
/// <item>Request AJAX → JSON <c>{ status, errorCode, message, errors }</c> để JS hiện thông báo.</item>
/// <item>Còn lại → trang <c>Views/Shared/ApiError.cshtml</c> với đúng HTTP status.</item>
/// </list>
/// </remarks>
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ApiSession _session;
    private readonly IModelMetadataProvider _metadataProvider;

    public ApiExceptionFilter(ApiSession session, IModelMetadataProvider metadataProvider)
    {
        _session = session;
        _metadataProvider = metadataProvider;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiException ex)
        {
            return;
        }

        var request = context.HttpContext.Request;

        if (ex.Status == StatusCodes.Status401Unauthorized)
        {
            await _session.SignOutAsync();
        }

        if (request.IsAjax())
        {
            context.Result = new JsonResult(new { status = ex.Status, errorCode = ex.ErrorCode, message = ex.Message, errors = ex.Errors })
            {
                StatusCode = ex.Status,
            };
        }
        else if (ex.Status == StatusCodes.Status401Unauthorized)
        {
            var returnUrl = $"{request.PathBase}{request.Path}{request.QueryString}";
            context.Result = new RedirectToActionResult("Login", "Account", new { area = string.Empty, returnUrl });
        }
        else
        {
            context.Result = new ViewResult
            {
                ViewName = "ApiError",
                StatusCode = ex.Status,
                ViewData = new ViewDataDictionary(_metadataProvider, context.ModelState) { Model = ex },
            };
        }

        context.ExceptionHandled = true;
    }
}
