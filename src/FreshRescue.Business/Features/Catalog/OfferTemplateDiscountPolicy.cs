using FreshRescue.Business.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>
/// BR-07: Biên độ của brand phải nằm trong biên độ nền tảng; giá đề xuất cũng phải
/// nằm trong cả hai biên độ. Dùng lại hàm này khi triển khai Import Excel BR-26.
/// </summary>
public static class OfferTemplateDiscountPolicy
{
    public static void Validate(
        OfferType offerType,
        decimal originalPrice,
        decimal suggestedPrice,
        decimal brandMinDiscountPercent,
        decimal brandMaxDiscountPercent)
    {
        if (originalPrice <= 0m || suggestedPrice <= 0m)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed,
                "Giá gốc và giá gợi ý phải lớn hơn 0.");
        }

        var (floorMin, floorMax) = offerType switch
        {
            OfferType.SurpriseBag => (65m, 75m),
            OfferType.ClearanceItem => (40m, 50m),
            _ => throw AppException.BadRequest(ErrorCodes.ValidationFailed,
                "Loại mẫu hàng không hợp lệ.")
        };

        if (brandMinDiscountPercent < floorMin
            || brandMaxDiscountPercent > floorMax
            || brandMinDiscountPercent > brandMaxDiscountPercent)
        {
            throw AppException.BadRequest(ErrorCodes.PriceOutOfBand,
                $"Biên độ giảm giá của brand phải nằm trong {floorMin}–{floorMax}% và min không vượt max.");
        }

        var discount = (originalPrice - suggestedPrice) * 100m / originalPrice;
        if (discount < floorMin || discount > floorMax
            || discount < brandMinDiscountPercent || discount > brandMaxDiscountPercent)
        {
            throw AppException.BadRequest(ErrorCodes.PriceOutOfBand,
                $"Giá gợi ý tạo mức giảm {decimal.Round(discount, 2)}%, ngoài biên độ {brandMinDiscountPercent}–{brandMaxDiscountPercent}% của brand.");
        }
    }
}
