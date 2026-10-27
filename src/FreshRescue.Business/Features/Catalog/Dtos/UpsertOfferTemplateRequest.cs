using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

// POST và PUT sử dụng cùng cấu trúc dữ liệu; BrandId luôn lấy từ JWT, không lấy từ request.
public sealed class UpsertOfferTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public OfferType OfferType { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal BrandMinDiscountPercent { get; set; }
    public decimal BrandMaxDiscountPercent { get; set; }
}
