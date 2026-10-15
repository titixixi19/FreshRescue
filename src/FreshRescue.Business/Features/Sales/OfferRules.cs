using FreshRescue.Business.Common;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Sales;

namespace FreshRescue.Business.Features.Sales;

/// <summary>
/// Luật dùng chung của <see cref="SaleBatchService"/> và <see cref="OfferService"/>: biên độ giá
/// hai tầng (BR-07), chụp lại dữ liệu từ template (BR-18), phạm vi chi nhánh (BR-10, BR-11).
/// </summary>
internal static class OfferRules
{
    // BR-07, tầng sàn: phần trăm giảm so với giá gốc.
    private static (decimal Min, decimal Max) FloorBand(OfferType type) => type switch
    {
        OfferType.SurpriseBag => (65m, 75m),
        OfferType.ClearanceItem => (40m, 50m),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>
    /// Giá phải nằm trong GIAO của tầng sàn và tầng brand. Brand để cả hai cột bằng 0 nghĩa là
    /// chưa khai tầng riêng — chỉ áp tầng sàn.
    /// </summary>
    public static void EnsurePriceInBand(
        OfferType type, decimal originalPrice, decimal actualPrice, decimal brandMinPercent, decimal brandMaxPercent)
    {
        var (min, max) = FloorBand(type);
        if (brandMinPercent != 0 || brandMaxPercent != 0)
        {
            min = Math.Max(min, brandMinPercent);
            max = Math.Min(max, brandMaxPercent);
        }

        // Giá VND là số nguyên (decimal(18,0)) nên làm tròn khoảng hợp lệ vào trong.
        var lowest = Math.Ceiling(originalPrice * (100m - max) / 100m);
        var highest = Math.Floor(originalPrice * (100m - min) / 100m);

        if (min > max || actualPrice < lowest || actualPrice > highest)
        {
            throw AppException.BadRequest(
                ErrorCodes.PriceOutOfBand,
                $"Giá bán phải từ {lowest:N0}đ đến {highest:N0}đ (giảm {min:0.##}–{max:0.##}% so với giá gốc {originalPrice:N0}đ).");
        }
    }

    /// <summary>BR-18: chụp lại tên, mô tả, ảnh, danh mục, loại, giá gốc từ template tại thời điểm tạo.</summary>
    public static Offer CreateFromTemplate(
        OfferTemplate template, Guid saleBatchId, decimal actualPrice, int totalQuantity, int? maxPerCustomer)
    {
        EnsurePriceInBand(
            template.OfferType, template.OriginalPrice, actualPrice,
            template.BrandMinDiscountPercent, template.BrandMaxDiscountPercent);

        return new Offer
        {
            SaleBatchId = saleBatchId,
            OfferTemplateId = template.Id,
            Name = template.Name,
            Description = template.Description,
            ImageUrl = template.ImageUrl,
            CategoryId = template.CategoryId,
            OfferType = template.OfferType,
            OriginalPrice = template.OriginalPrice,
            ActualPrice = actualPrice,
            TotalQuantity = totalQuantity,
            RemainingQuantity = totalQuantity,
            MaxPerCustomer = maxPerCustomer ?? Offer.DefaultMaxPerCustomer,
        };
    }

    /// <summary>BR-10 / BR-11: Staff đúng chi nhánh của mình, Brand Manager đúng thương hiệu của mình.</summary>
    public static void EnsureInStoreScope(ICurrentUser user, Store store)
    {
        var inScope = (user.IsInRole(AppRoles.StoreStaff) && user.StoreId == store.Id)
            || (user.IsInRole(AppRoles.BrandManager) && user.BrandId == store.BrandId);

        if (!inScope)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                $"Chi nhánh '{store.Name}' không thuộc phạm vi của bạn.");
        }
    }

    /// <summary>Gán Kind = Utc: CSDL lưu UTC, request có thể đến dưới dạng Unspecified hoặc Local.</summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    /// <summary>BR-15.</summary>
    public static void EnsureTimesValid(DateTime saleStart, DateTime saleEnd, DateTime pickupStart, DateTime pickupEnd)
    {
        var valid = saleStart < saleEnd
            && saleEnd <= pickupEnd
            && pickupStart < pickupEnd
            && saleStart <= pickupStart;

        if (!valid)
        {
            throw AppException.BadRequest(
                ErrorCodes.BatchTimeInvalid,
                "Mốc thời gian không hợp lệ: cần SaleStart < SaleEnd ≤ PickupEnd, PickupStart < PickupEnd và SaleStart ≤ PickupStart.");
        }
    }
}
