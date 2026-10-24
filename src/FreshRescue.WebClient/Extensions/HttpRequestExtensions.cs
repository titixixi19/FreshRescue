namespace FreshRescue.WebClient.Extensions;

public static class HttpRequestExtensions
{
    /// <summary>JS của WebClient gửi header này với mọi lệnh <c>fetch</c> (wwwroot/js/site.js).</summary>
    public static bool IsAjax(this HttpRequest request)
        => string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal);
}
