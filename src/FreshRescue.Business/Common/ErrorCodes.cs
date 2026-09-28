namespace FreshRescue.Business.Common;

/// <summary>
/// Đủ 28 mã lỗi, chép nguyên từ 03-API-CONTRACT.md §1.4. Chủ sở hữu: Tùng.
/// Thiếu mã ⇒ contract thiếu ⇒ sửa contract trước rồi mới thêm vào đây (07-WORK-BREAKDOWN.md §12.1).
/// Client bắt lỗi theo các chuỗi này — đổi một chuỗi là phá hợp đồng.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string ForbiddenScope = "FORBIDDEN_SCOPE";
    public const string NotFound = "NOT_FOUND";
    public const string BrandNotApproved = "BRAND_NOT_APPROVED";
    public const string BatchNotActive = "BATCH_NOT_ACTIVE";
    public const string SaleWindowClosed = "SALE_WINDOW_CLOSED";
    public const string OfferSoldOut = "OFFER_SOLD_OUT";
    public const string OfferCrossBatch = "OFFER_CROSS_BATCH";
    public const string PurchaseLimitExceeded = "PURCHASE_LIMIT_EXCEEDED";
    public const string PriceOutOfBand = "PRICE_OUT_OF_BAND";
    public const string PaymentDeadlinePassed = "PAYMENT_DEADLINE_PASSED";
    public const string OrderNotPayable = "ORDER_NOT_PAYABLE";
    public const string CancelWindowPassed = "CANCEL_WINDOW_PASSED";
    public const string RedeemWindowClosed = "REDEEM_WINDOW_CLOSED";
    public const string OrderAlreadyRedeemed = "ORDER_ALREADY_REDEEMED";
    public const string QrInvalidSignature = "QR_INVALID_SIGNATURE";
    public const string QrExpired = "QR_EXPIRED";
    public const string BatchTimeInvalid = "BATCH_TIME_INVALID";
    public const string BatchOverlap = "BATCH_OVERLAP";
    public const string BatchLocked = "BATCH_LOCKED";
    public const string BatchHasRedeemedOrders = "BATCH_HAS_REDEEMED_ORDERS";
    public const string OfferHasOrders = "OFFER_HAS_ORDERS";
    public const string ReviewAlreadyExists = "REVIEW_ALREADY_EXISTS";
    public const string DisputeWindowPassed = "DISPUTE_WINDOW_PASSED";
    public const string DisputeAlreadyExists = "DISPUTE_ALREADY_EXISTS";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
}
