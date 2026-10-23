namespace FreshRescue.WebClient.Services;

/// <summary>
/// Mã lỗi phía client cần bắt riêng, và câu hiển thị dự phòng khi API không gửi <c>detail</c>.
/// Câu chữ theo bảng "Ý nghĩa với người dùng" ở 03-API-CONTRACT.md §4.1.
/// </summary>
public static class ApiErrorMessages
{
    // Mã của API — chép từ 03 §1.4, chỉ những mã WebClient xử lý riêng.
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string OfferSoldOut = "OFFER_SOLD_OUT";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    /// <summary>Mã riêng của WebClient, không có trong contract: không kết nối được tới API.</summary>
    public const string ApiUnavailable = "API_UNAVAILABLE";

    private static readonly Dictionary<string, string> _byCode = new(StringComparer.Ordinal)
    {
        [Unauthenticated] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        ["FORBIDDEN_SCOPE"] = "Bạn không có quyền thao tác trên dữ liệu này.",
        [NotFound] = "Không tìm thấy dữ liệu.",
        [ValidationFailed] = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại.",
        ["BATCH_NOT_ACTIVE"] = "Đợt bán chưa mở hoặc đã đóng.",
        ["SALE_WINDOW_CLOSED"] = "Đợt bán đã kết thúc.",
        [OfferSoldOut] = "Có người vừa mua mất — vui lòng tải lại danh sách.",
        ["OFFER_CROSS_BATCH"] = "Các món trong một đơn phải cùng một đợt bán.",
        ["PURCHASE_LIMIT_EXCEEDED"] = "Mỗi người chỉ mua tối đa 3 suất cho một món trong một đợt.",
        ["PAYMENT_DEADLINE_PASSED"] = "Đã quá hạn thanh toán, đơn sẽ được huỷ tự động.",
        ["ORDER_NOT_PAYABLE"] = "Đơn này không còn chờ thanh toán.",
        ["CANCEL_WINDOW_PASSED"] = "Đã quá hạn huỷ đơn.",
        ["ORDER_ALREADY_REDEEMED"] = "Đơn đã được nhận hàng.",
        ["REVIEW_ALREADY_EXISTS"] = "Bạn đã đánh giá đơn này rồi.",
        ["DISPUTE_WINDOW_PASSED"] = "Chỉ khiếu nại được trong 2 giờ sau khi nhận hàng.",
        ["DISPUTE_ALREADY_EXISTS"] = "Đơn này đã có khiếu nại.",
        [ConcurrencyConflict] = "Hệ thống đang bận, vui lòng thử lại.",
        [ApiUnavailable] = "Không kết nối được tới máy chủ. Vui lòng thử lại sau.",
    };

    /// <summary>Ưu tiên <c>detail</c> của API (cụ thể hơn), không có thì tra theo mã, rồi theo HTTP status.</summary>
    public static string Resolve(int status, string? errorCode, string? detail)
    {
        if (!string.IsNullOrWhiteSpace(detail))
        {
            return detail;
        }

        if (errorCode is not null && _byCode.TryGetValue(errorCode, out var message))
        {
            return message;
        }

        return status switch
        {
            StatusCodes.Status401Unauthorized => _byCode[Unauthenticated],
            StatusCodes.Status403Forbidden => "Bạn không có quyền thực hiện thao tác này.",
            StatusCodes.Status404NotFound => _byCode[NotFound],
            >= 500 => "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.",
            _ => "Yêu cầu không hợp lệ.",
        };
    }
}
