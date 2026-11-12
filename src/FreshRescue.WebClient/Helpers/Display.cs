using System.Globalization;
using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Helpers;

/// <summary>
/// Định dạng hiển thị dùng chung cho view. API trả giờ UTC (OQ-33), người dùng đọc giờ Việt Nam.
/// </summary>
public static class Display
{
    private static readonly CultureInfo _vi = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo _vietnam = FindVietnamTimeZone();

    /// <summary><c>35000</c> → <c>35.000₫</c>. Tiền là số nguyên VND (03 §1).</summary>
    public static string Money(decimal amount) => amount.ToString("N0", _vi) + "₫";

    public static DateTime ToVietnamTime(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _vietnam);

    /// <summary>
    /// Giờ tường Việt Nam (từ datetime-local, Kind Unspecified/Local) → UTC gửi API.
    /// Không dùng SpecifyKind(..., Utc) vì chỉ đổi nhãn, không đổi mốc thời gian.
    /// </summary>
    public static DateTime FromVietnamToUtc(DateTime vietnamWallClock)
    {
        // datetime-local bind thành Unspecified; coi là giờ trên đồng hồ VN.
        var unspecified = DateTime.SpecifyKind(vietnamWallClock, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _vietnam);
    }

    public static string Time(DateTime utc) => ToVietnamTime(utc).ToString("HH:mm", _vi);

    public static string DateAndTime(DateTime utc) => ToVietnamTime(utc).ToString("HH:mm dd/MM/yyyy", _vi);

    /// <summary><c>21:00–21:45 16/10</c> — khung giờ nhận hàng.</summary>
    public static string Window(DateTime startUtc, DateTime endUtc)
        => $"{Time(startUtc)}–{Time(endUtc)} {ToVietnamTime(startUtc).ToString("dd/MM", _vi)}";

    /// <summary>Cho thuộc tính <c>data-*</c> để JS đếm ngược: luôn có hậu tố Z.</summary>
    public static string Iso(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    public static string Percent(double value) => value.ToString("0", _vi) + "%";

    public static string Distance(double km)
        => km < 1 ? $"{km * 1000:0} m" : km.ToString("0.0", _vi) + " km";

    public static (string Label, string Css) Status(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => ("Chờ thanh toán", "bg-warning text-dark"),
        OrderStatus.Paid => ("Đã thanh toán — chờ nhận", "bg-primary"),
        OrderStatus.Redeemed => ("Đã nhận hàng", "bg-success"),
        OrderStatus.Cancelled => ("Đã huỷ", "bg-secondary"),
        OrderStatus.NoShow => ("Không đến nhận", "bg-danger"),
        _ => (status.ToString(), "bg-light text-dark"),
    };

    public static string OfferTypeLabel(OfferType type) => type switch
    {
        OfferType.SurpriseBag => "Túi mù",
        OfferType.ClearanceItem => "Xả kho",
        _ => type.ToString(),
    };

    public static string CancelledByLabel(CancelledBy? by) => by switch
    {
        CancelledBy.Customer => "Bạn đã huỷ",
        CancelledBy.Store => "Cửa hàng huỷ",
        CancelledBy.System => "Hệ thống tự huỷ",
        _ => "Đã huỷ",
    };

    public static string DisputeReasonLabel(DisputeReason reason) => reason switch
    {
        DisputeReason.Spoiled => "Đồ ăn bị hỏng, ôi thiu",
        DisputeReason.WrongCategory => "Sai loại (ví dụ đặt đồ chay nhận đồ mặn)",
        DisputeReason.Missing => "Thiếu món",
        DisputeReason.Other => "Lý do khác",
        _ => reason.ToString(),
    };

    // Dashboard nhận trạng thái dạng chuỗi enum của API (03 §1); đổi sang chữ người dùng đọc được,
    // kèm class badge của dashboard.css. Giá trị lạ thì hiện nguyên văn thay vì ném lỗi.

    public static (string Label, string Css) BrandStatus(string? status) => status switch
    {
        "PendingApproval" => ("Chờ duyệt", "badge"),
        "Approved" => ("Đã duyệt", "badge badge-ok"),
        "Rejected" => ("Bị từ chối", "badge badge-warn"),
        "Suspended" => ("Tạm khoá", "badge badge-warn"),
        _ => (status ?? "—", "badge"),
    };

    public static (string Label, string Css) SaleBatchStatus(string? status) => status switch
    {
        "Draft" => ("Bản nháp", "badge"),
        "Scheduled" => ("Đã lên lịch", "badge"),
        "Active" => ("Đang bán", "badge badge-ok"),
        "Closed" => ("Đã đóng", "badge"),
        "Cancelled" => ("Đã huỷ", "badge badge-warn"),
        _ => (status ?? "—", "badge"),
    };

    public static (string Label, string Css) DisputeStatus(string? status) => status switch
    {
        "Open" => ("Chờ xử lý", "badge"),
        "Approved" => ("Đã hoàn tiền", "badge badge-ok"),
        "Rejected" => ("Đã từ chối", "badge badge-warn"),
        _ => (status ?? "—", "badge"),
    };

    public static string RoleLabel(string? role) => role switch
    {
        "SystemAdmin" => "Quản trị sàn",
        "BrandManager" => "Quản lý thương hiệu",
        "StoreStaff" => "Nhân viên cửa hàng",
        "Customer" => "Khách hàng",
        _ => role ?? "—",
    };

    // Windows dùng tên riêng, Linux/macOS dùng tên IANA — thử cả hai để chạy được khi deploy.
    private static TimeZoneInfo FindVietnamTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("ICT", TimeSpan.FromHours(7), "Giờ Việt Nam", "Giờ Việt Nam");
    }
}
