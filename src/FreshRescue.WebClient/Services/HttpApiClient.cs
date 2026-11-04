using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Services;

/// <summary>
/// Gọi FreshRescue.API thật. Tự gắn Bearer token, và tự đổi refresh token khi access token
/// (sống 15 phút — 03-API-CONTRACT.md §1.5) sắp hết hoặc bị API từ chối.
/// </summary>
public class HttpApiClient : IApiClient
{
    private const string RefreshPath = "auth/refresh";

    // Đổi token sớm một chút để request không hết hạn giữa đường.
    private static readonly TimeSpan _refreshAhead = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly ApiSession _session;
    private readonly TokenRefreshCoordinator _refreshCoordinator;
    private readonly TimeProvider _clock;
    private readonly ILogger<HttpApiClient> _logger;

    public HttpApiClient(
        HttpClient http,
        ApiSession session,
        TokenRefreshCoordinator refreshCoordinator,
        TimeProvider clock,
        ILogger<HttpApiClient> logger)
    {
        _http = http;
        _session = session;
        _refreshCoordinator = refreshCoordinator;
        _clock = clock;
        _logger = logger;
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        => SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken = default)
        => SendAsync<T>(() => WithJson(HttpMethod.Post, path, body), cancellationToken);

    public async Task PostAsync(string path, object? body, CancellationToken cancellationToken = default)
    {
        using var response = await SendWithTokenAsync(() => WithJson(HttpMethod.Post, path, body), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<T> PutAsync<T>(string path, object? body, CancellationToken cancellationToken = default)
        => SendAsync<T>(() => WithJson(HttpMethod.Put, path, body), cancellationToken);

    public Task<T> PatchAsync<T>(string path, object? body, CancellationToken cancellationToken = default)
        => SendAsync<T>(() => WithJson(HttpMethod.Patch, path, body), cancellationToken);

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Delete, path), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<UploadImageResponse> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        return SendAsync<UploadImageResponse>(
            () =>
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

                var form = new MultipartFormDataContent
                {
                    { fileContent, "file", file.FileName },
                    { new StringContent(folder), "folder" },
                };

                return new HttpRequestMessage(HttpMethod.Post, "uploads/images") { Content = form };
            },
            cancellationToken);
    }

    private static HttpRequestMessage WithJson(HttpMethod method, string path, object? body)
    {
        return new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body, body.GetType(), options: ApiJson.Options),
        };
    }

    private async Task<T> SendAsync<T>(Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        using var response = await SendWithTokenAsync(createRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        return await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, cancellationToken)
            ?? throw new ApiException((int)response.StatusCode, null, "API trả về nội dung rỗng.");
    }

    // Nhận một hàm tạo request thay vì một request, vì HttpRequestMessage không gửi lại được —
    // khi phải thử lại sau lúc đổi token thì cần tạo bản mới.
    private async Task<HttpResponseMessage> SendWithTokenAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken)
    {
        var tokens = await _session.GetTokensAsync();
        if (tokens is not null && tokens.ExpiresAt - _refreshAhead <= _clock.GetUtcNow().UtcDateTime)
        {
            tokens = await RefreshAsync(tokens, cancellationToken);
        }

        var response = await SendOnceAsync(createRequest, tokens?.AccessToken, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || tokens is null)
        {
            return response;
        }

        // Token bị từ chối dù chưa tới hạn (đồng hồ lệch, khoá ký đổi…) — đổi một lần rồi thử lại đúng một lần.
        response.Dispose();
        tokens = await RefreshAsync(tokens, cancellationToken);

        return await SendOnceAsync(createRequest, tokens?.AccessToken, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> createRequest,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        using var request = createRequest();
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        try
        {
            return await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không gọi được API. BaseAddress={BaseAddress}, Path={Path}", _http.BaseAddress, request.RequestUri);
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                ApiErrorMessages.ApiUnavailable,
                ApiErrorMessages.Resolve(StatusCodes.Status503ServiceUnavailable, ApiErrorMessages.ApiUnavailable, null));
        }
    }

    /// <summary>Đổi refresh token (xoay vòng — 03 §1.5). Thất bại thì đăng xuất và trả null.</summary>
    private async Task<ApiTokens?> RefreshAsync(ApiTokens tokens, CancellationToken cancellationToken)
    {
        var auth = await _refreshCoordinator.RefreshAsync(
            tokens.RefreshToken,
            () => RequestRefreshAsync(tokens.RefreshToken, cancellationToken),
            cancellationToken);

        if (auth is null)
        {
            await _session.SignOutAsync();
            return null;
        }

        await _session.SignInAsync(auth);
        return await _session.GetTokensAsync();
    }

    private async Task<AuthResponse?> RequestRefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await SendOnceAsync(
            () => WithJson(HttpMethod.Post, RefreshPath, new RefreshTokenRequest(refreshToken)),
            null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Refresh token bị từ chối, đăng xuất. Status={Status}", (int)response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>(ApiJson.Options, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var status = (int)response.StatusCode;
        string? errorCode = null;
        string? detail = null;
        Dictionary<string, string[]>? errors = null;

        // Không phải lỗi nào cũng có thân ProblemDetails (ví dụ 401 của JwtBearer, 404 của routing).
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var json = JsonDocument.Parse(raw);
                var root = json.RootElement;
                errorCode = ReadString(root, "errorCode");
                detail = ReadString(root, "detail") ?? (errorCode is null ? null : ReadString(root, "title"));
                if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    errors = errorsElement.Deserialize<Dictionary<string, string[]>>(ApiJson.Options);
                }
            }
            catch (JsonException)
            {
                // Thân không phải JSON — giữ câu mặc định theo status.
            }
        }

        if (status == StatusCodes.Status401Unauthorized)
        {
            errorCode ??= ApiErrorMessages.Unauthenticated;
        }

        throw new ApiException(status, errorCode, ApiErrorMessages.Resolve(status, errorCode, detail), errors);
    }

    public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await SendWithTokenAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        // Đọc stream phản hồi dưới dạng Stream (Caller tự dispose stream/response nếu cần, hoặc đọc bytes)
        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<T> UploadFileAsync<T>(string path, IFormFile file, string paramName = "file", CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(
            () =>
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

                var form = new MultipartFormDataContent
                {
                    { fileContent, paramName, file.FileName }
                };

                return new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
            },
            cancellationToken);
    }
    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
