using System.Text.Json;
using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;

namespace FreshRescue.WebClient.Areas.Customer.Models;

/// <summary>
/// Dữ liệu cho nút mở modal Quick Checkout (<c>data-qc</c>, đọc bởi wwwroot/js/checkout.js).
/// Razor tự mã hoá chuỗi trả về khi đặt vào thuộc tính HTML.
/// </summary>
public static class QuickCheckout
{
    public static string For(ActiveOfferDto offer) => For(offer, [offer]);

    /// <summary>Mở sẵn mọi món cùng đợt; món đang xem chọn sẵn 1 suất.</summary>
    public static string For(ActiveOfferDto selected, IEnumerable<ActiveOfferDto> batchOffers)
    {
        var data = new
        {
            selected.SaleBatchId,
            selected.StoreName,
            selected.PickupStartTime,
            selected.PickupEndTime,
            Offers = batchOffers
                .Where(o => o.SaleBatchId == selected.SaleBatchId)
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.ActualPrice,
                    o.OriginalPrice,
                    o.RemainingQuantity,
                    o.MaxPerCustomer,
                    Quantity = o.Id == selected.Id && o.RemainingQuantity > 0 ? 1 : 0,
                }),
        };

        return JsonSerializer.Serialize(data, ApiJson.Options);
    }
}
