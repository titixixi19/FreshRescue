using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

public sealed class OfferTemplateDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public OfferType OfferType { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal BrandMinDiscountPercent { get; set; }
    public decimal BrandMaxDiscountPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
