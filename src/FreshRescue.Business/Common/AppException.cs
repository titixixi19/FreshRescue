namespace FreshRescue.Business.Common;

/// <summary>
/// Cách duy nhất để service báo lỗi nghiệp vụ. Middleware ở API dịch sang ProblemDetails
/// (03-API-CONTRACT.md §1.3). Không <c>return BadRequest(...)</c> ở controller.
/// </summary>
/// <example>
/// <code>throw AppException.Conflict(ErrorCodes.OfferSoldOut, "'Túi mù bánh ngọt' chỉ còn 1 suất.");</code>
/// </example>
public class AppException : Exception
{
    public string ErrorCode { get; }

    public int StatusCode { get; }

    private AppException(string errorCode, int statusCode, string detail)
        : base(detail)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public static AppException BadRequest(string code, string detail) => new(code, 400, detail);

    public static AppException Unauthorized(string code, string detail) => new(code, 401, detail);

    public static AppException Forbidden(string code, string detail) => new(code, 403, detail);

    public static AppException NotFound(string code, string detail) => new(code, 404, detail);

    public static AppException Conflict(string code, string detail) => new(code, 409, detail);
}
