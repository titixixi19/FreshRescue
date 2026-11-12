using System.Globalization;
using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Customer.Models;

/// <summary>Bộ lọc storefront, ánh xạ 1-1 sang query của <c>GET /offers/active</c> (FR-C03).</summary>
public class StorefrontQuery
{
    /// <summary>Xem các món của một chi nhánh — link từ trang chi tiết offer.</summary>
    public Guid? StoreId { get; set; }
    public Guid? CategoryId { get; set; }
    public OfferType? OfferType { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }

    /// <summary>Lấy từ trình duyệt bằng Geolocation, cả hai cùng có hoặc cùng không.</summary>
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public double? RadiusKm { get; set; }
    public int Page { get; set; } = 1;

    public bool HasLocation => Lat is not null && Lng is not null;

    public bool HasFilter => StoreId is not null || CategoryId is not null || OfferType is not null || MinPrice is not null
        || MaxPrice is not null || InStockOnly || HasLocation;

    public Dictionary<string, string?> ToApiQuery(int pageSize)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = Math.Max(Page, 1).ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };

        Add(query, "storeId", StoreId?.ToString());
        Add(query, "categoryId", CategoryId?.ToString());
        Add(query, "offerType", OfferType?.ToString());
        Add(query, "minPrice", MinPrice?.ToString(CultureInfo.InvariantCulture));
        Add(query, "maxPrice", MaxPrice?.ToString(CultureInfo.InvariantCulture));
        Add(query, "inStockOnly", InStockOnly ? "true" : null);

        if (HasLocation)
        {
            Add(query, "lat", Lat!.Value.ToString("0.######", CultureInfo.InvariantCulture));
            Add(query, "lng", Lng!.Value.ToString("0.######", CultureInfo.InvariantCulture));
            Add(query, "radiusKm", (RadiusKm ?? 5).ToString(CultureInfo.InvariantCulture));
        }

        return query;
    }

    /// <summary>Tham số route để link phân trang giữ nguyên bộ lọc.</summary>
    public Dictionary<string, string?> ToRouteValues(int page)
    {
        var values = ToApiQuery(0);
        values.Remove("pageSize");
        values["page"] = page.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    private static void Add(Dictionary<string, string?> query, string key, string? value)
    {
        if (value is not null)
        {
            query[key] = value;
        }
    }
}

public class StorefrontViewModel
{
    public required StorefrontQuery Query { get; init; }
    public required PagedResponse<ActiveOfferDto> Offers { get; init; }
    public required IReadOnlyList<CategoryDto> Categories { get; init; }

    /// <summary>Thống kê toàn sàn cho banner — không đổi theo bộ lọc đang chọn.</summary>
    public required SiteRescueStats Stats { get; init; }
}

/// <param name="OfferCount">Số món đang mở bán và còn suất.</param>
/// <param name="PortionCount">Tổng suất còn lại của các món đó.</param>
/// <param name="IsPartial">Có hơn 100 món nên chỉ cộng được suất của 100 món đầu — hiển thị kèm dấu "+".</param>
public record SiteRescueStats(int OfferCount, int PortionCount, bool IsPartial);

public class OfferDetailViewModel
{
    public required ActiveOfferDto Offer { get; init; }

    /// <summary>Các món cùng đợt bán — một đơn được mua nhiều món nhưng chỉ trong một đợt (BR-03).</summary>
    public required IReadOnlyList<ActiveOfferDto> BatchOffers { get; init; }
    public bool IsOnSale { get; init; }
}
