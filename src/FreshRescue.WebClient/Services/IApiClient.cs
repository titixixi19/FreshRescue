using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Services;

/// <summary>
/// Cổng duy nhất để WebClient gọi API (07-WORK-BREAKDOWN.md §12.3). Chủ sở hữu: Đức Anh —
/// đã đóng băng, cần thêm method thì nhắn trước.
/// </summary>
/// <remarks>
/// <para>
/// <c>path</c> tương đối với <c>{host}/api/v1/</c> và không có dấu <c>/</c> đầu: <c>"me/orders?page=2"</c>.
/// Ghép query bằng <c>QueryHelpers.AddQueryString</c>, đừng nối chuỗi tay.
/// </para>
/// <para>
/// Lỗi trả về luôn là <see cref="ApiException"/> mang <c>errorCode</c> của contract. Lỗi không bắt
/// thì <c>ApiExceptionFilter</c> lo: 401 về trang đăng nhập, còn lại hiện trang lỗi hoặc JSON.
/// </para>
/// <para>
/// Hai bản cài đặt, chọn bằng cờ <c>UseMockApi</c>: <see cref="HttpApiClient"/> gọi API thật,
/// <see cref="MockApiClient"/> trả dữ liệu giả theo đúng ví dụ trong contract.
/// </para>
/// </remarks>
public interface IApiClient
{
    Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default);

    Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken = default);

    /// <summary>Cho endpoint mà nội dung trả về không cần dùng.</summary>
    Task PostAsync(string path, object? body, CancellationToken cancellationToken = default);

    Task<T> PutAsync<T>(string path, object? body, CancellationToken cancellationToken = default);

    Task<T> PatchAsync<T>(string path, object? body, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>POST /uploads/images</c> (03 §12). <paramref name="folder"/> ∈ offers · reviews · disputes ·
    /// avatars · logos. Trả đường dẫn tương đối để gửi kèm request tiếp theo.
    /// </summary>
    Task<UploadImageResponse> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải file từ API (dùng cho export excel hoặc tải template mẫu).
    /// </summary>
    Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi file (multipart/form-data) lên API (dùng cho import offer-templates từ file .xlsx).
    /// </summary>
    Task<T> UploadFileAsync<T>(string path, IFormFile file, string paramName = "file", CancellationToken cancellationToken = default);

}
