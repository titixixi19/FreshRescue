using System.Security.Claims;
using System.Text.Json;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services.Mock;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace FreshRescue.WebClient.Services;

/// <summary>
/// Bản giả của <see cref="IApiClient"/> — bật bằng <c>"UseMockApi": true</c> (07-WORK-BREAKDOWN.md §12.3).
/// Giao diện chạy được khi endpoint chưa có, và vì kết quả đi qua JSON một vòng, tên trường
/// sai so với contract sẽ lộ ra ngay từ lúc dùng mock chứ không đợi tới lúc ghép API thật.
/// </summary>
/// <remarks>
/// Cần giả lập thêm endpoint (ví dụ màn Dashboard): thêm một nhánh vào <see cref="Route"/> và
/// dữ liệu vào <see cref="MockApiStore"/>. Endpoint chưa có nhánh trả <c>404 NOT_FOUND</c> kèm
/// tên endpoint, để biết ngay còn thiếu gì.
/// </remarks>
public class MockApiClient : IApiClient
{
    private static readonly string[] _imageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly MockApiStore _store;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MockApiClient(MockApiStore store, IHttpContextAccessor httpContextAccessor)
    {
        _store = store;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        => Task.FromResult(RoundTrip<T>(Route(HttpMethods.Get, path, null)));

    public Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken = default)
        => Task.FromResult(RoundTrip<T>(Route(HttpMethods.Post, path, body)));

    public Task PostAsync(string path, object? body, CancellationToken cancellationToken = default)
    {
        Route(HttpMethods.Post, path, body);
        return Task.CompletedTask;
    }

    public Task<T> PutAsync<T>(string path, object? body, CancellationToken cancellationToken = default)
        => Task.FromResult(RoundTrip<T>(Route(HttpMethods.Put, path, body)));

    public Task<T> PatchAsync<T>(string path, object? body, CancellationToken cancellationToken = default)
        => Task.FromResult(RoundTrip<T>(Route(HttpMethods.Patch, path, body)));

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        Route(HttpMethods.Delete, path, null);
        return Task.CompletedTask;
    }

    public Task<UploadImageResponse> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        RequireUser();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_imageExtensions.Contains(extension) || file.Length is 0 or > MaxImageBytes)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                ApiErrorMessages.ValidationFailed,
                "Chỉ nhận ảnh .jpg, .jpeg, .png, .webp, tối đa 5 MB.");
        }

        // Không lưu file thật — chỉ trả đường dẫn tương đối đúng khuôn 03 §12.
        return Task.FromResult(new UploadImageResponse
        {
            Url = $"/uploads/{folder}/{Guid.NewGuid():N}{extension}",
            SizeBytes = file.Length,
        });
    }

    private object Route(string method, string path, object? body)
    {
        var questionMark = path.IndexOf('?', StringComparison.Ordinal);
        var query = questionMark < 0
            ? new Dictionary<string, StringValues>()
            : QueryHelpers.ParseQuery(path[questionMark..]);
        var segments = (questionMark < 0 ? path : path[..questionMark])
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        return (method, segments) switch
        {
            ("POST", ["auth", "login"]) => _store.Login(Read<LoginRequest>(body)),
            ("POST", ["auth", "register"]) => _store.Register(Read<RegisterCustomerRequest>(body)),
            ("POST", ["auth", "refresh"]) => _store.Refresh(Read<RefreshTokenRequest>(body)),
            ("POST", ["auth", "logout"]) => _store.Logout(Read<RefreshTokenRequest>(body)),
            ("GET", ["auth", "me"]) => _store.GetUser(RequireUser()),

            ("GET", ["categories"]) => _store.GetCategories(),
            ("GET", ["offers", "active"]) => _store.GetActiveOffers(query),
            ("GET", ["offers", var id]) => _store.GetOffer(ParseId(id)),

            ("POST", ["orders"]) => _store.CreateOrder(RequireCustomer(), Read<CreateOrderRequest>(body)),
            ("POST", ["orders", var id, "payment"]) => _store.Pay(RequireCustomer(), ParseId(id), Read<PayOrderRequest>(body)),
            ("POST", ["orders", var id, "cancel"]) => _store.Cancel(RequireCustomer(), ParseId(id)),
            ("GET", ["orders", var id, "qr"]) => _store.GetQrTicket(RequireCustomer(), ParseId(id)),
            ("POST", ["orders", var id, "review"]) => _store.CreateReview(RequireCustomer(), ParseId(id), Read<CreateReviewRequest>(body)),
            ("POST", ["orders", var id, "dispute"]) => _store.CreateDispute(RequireCustomer(), ParseId(id), Read<CreateDisputeRequest>(body)),
            ("GET", ["me", "orders"]) => _store.GetMyOrders(RequireCustomer(), query),
            ("GET", ["me", "orders", var id]) => _store.GetMyOrder(RequireCustomer(), ParseId(id)),

            _ => throw new ApiException(
                StatusCodes.Status404NotFound,
                ApiErrorMessages.NotFound,
                $"MockApiClient chưa giả lập {method} /api/v1/{path}."),
        };
    }

    private Guid RequireUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new ApiException(StatusCodes.Status401Unauthorized, ApiErrorMessages.Unauthenticated, ApiErrorMessages.Resolve(401, null, null));
    }

    private Guid RequireCustomer()
    {
        var id = RequireUser();
        return _httpContextAccessor.HttpContext!.User.IsInRole(AppRoles.Customer)
            ? id
            : throw new ApiException(StatusCodes.Status403Forbidden, null, ApiErrorMessages.Resolve(403, null, null));
    }

    private static Guid ParseId(string value)
        => Guid.TryParse(value, out var id)
            ? id
            : throw new ApiException(StatusCodes.Status404NotFound, ApiErrorMessages.NotFound, ApiErrorMessages.Resolve(404, null, null));

    // Đi qua JSON như request thật: body là kiểu ẩn danh hay record khác tên đều đọc được nếu đúng tên trường.
    private static T Read<T>(object? body)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(body, ApiJson.Options), ApiJson.Options)
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                ApiErrorMessages.ValidationFailed,
                ApiErrorMessages.Resolve(400, ApiErrorMessages.ValidationFailed, null));
        }
    }

    private static T RoundTrip<T>(object result)
        => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(result, ApiJson.Options), ApiJson.Options)!;

    public Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        // Trả về một stream rỗng hoặc dữ liệu giả lập cho file Excel (.xlsx)
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Mock Excel File Content"));
        return Task.FromResult<Stream>(stream);
    }

    public Task<T> UploadFileAsync<T>(string path, IFormFile file, string paramName = "file", CancellationToken cancellationToken = default)
    {
        RequireUser();
        // Kiểm tra định dạng đơn giản cho mock import
        if (file.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, ApiErrorMessages.ValidationFailed, "File tải lên trống.");
        }

        // Giả lập kết quả trả về thành công khi import
        var mockResult = new { ImportedCount = 10 };
        var result = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(mockResult, ApiJson.Options), ApiJson.Options)!;
        return Task.FromResult(result);
    }
}
