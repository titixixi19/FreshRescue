namespace FreshRescue.WebClient.Services;

/// <summary>
/// Lỗi từ API, đọc từ khuôn ProblemDetails (03-API-CONTRACT.md §1.3). Bắt theo
/// <see cref="ErrorCode"/>, không bắt theo câu chữ — <see cref="Exception.Message"/> chỉ để hiển thị.
/// </summary>
public class ApiException : Exception
{
    public ApiException(
        int status,
        string? errorCode,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Status = status;
        ErrorCode = errorCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public int Status { get; }

    /// <summary>Mã trong 03 §1.4, hoặc mã riêng của client ở <see cref="ApiErrorMessages"/>. Có thể null khi API không gửi.</summary>
    public string? ErrorCode { get; }

    /// <summary>Lỗi theo từng trường khi <c>VALIDATION_FAILED</c>, khoá dạng camelCase: <c>items[0].quantity</c>.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public bool Is(string errorCode) => string.Equals(ErrorCode, errorCode, StringComparison.Ordinal);
}
