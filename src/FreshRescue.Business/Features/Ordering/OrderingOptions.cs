namespace FreshRescue.Business.Features.Ordering;

/// <summary>Mục <c>Ordering</c> trong appsettings.</summary>
public class OrderingOptions
{
    public const string SectionName = "Ordering";

    public OfferLockingStrategy LockingStrategy { get; set; } = OfferLockingStrategy.Pessimistic;
}

/// <summary>
/// Cách giữ tồn kho khi đặt hàng (02-DOMAIN-MODEL.md §6). Giữ cả hai để đo so sánh trong báo cáo.
/// </summary>
public enum OfferLockingStrategy
{
    /// <summary>
    /// Mặc định. Khoá dòng offer (UPDLOCK) tới hết giao dịch — các đơn xếp hàng chờ nhau, nên
    /// 50 người tranh 10 suất thì đúng 10 người được (NFR-01).
    /// </summary>
    Pessimistic,

    /// <summary>
    /// RowVersion + thử lại 3 lần. Nhanh khi ít tranh chấp, nhưng khi đông người có thể trả
    /// CONCURRENCY_CONFLICT dù kho vẫn còn.
    /// </summary>
    Optimistic,
}
