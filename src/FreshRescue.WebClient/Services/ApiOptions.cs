namespace FreshRescue.WebClient.Services;

/// <summary>Mục <c>Api</c> trong appsettings.json.</summary>
public class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>Host của FreshRescue.API, không kèm <c>/api/v1</c>.</summary>
    public string BaseUrl { get; set; } = "https://localhost:7001";

    /// <summary>Gốc của mọi đường dẫn mà <see cref="IApiClient"/> nhận.</summary>
    public Uri ApiRoot => new($"{BaseUrl.TrimEnd('/')}/api/v1/");

    /// <summary>
    /// API trả ảnh bằng đường dẫn tương đối (OQ-09) — client tự ghép host, để đổi host hay
    /// chuyển CDN thì dữ liệu cũ không hỏng.
    /// </summary>
    public string? ResolveUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Uri.IsWellFormedUriString(path, UriKind.Absolute)
            ? path
            : $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
