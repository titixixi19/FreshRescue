namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>Chỉ chứa các trường công khai, an toàn để truy vấn OData.</summary>
public sealed class StoreODataDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
}
