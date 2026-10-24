namespace FreshRescue.API.OData;

/// <summary>Public, read-only offer projection; do not expose EF entities directly.</summary>
public sealed class OfferODataDto
{
    public Guid Id { get; set; }
    public Guid SaleBatchId { get; set; }
    public Guid StoreId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string OfferType { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal ActualPrice { get; set; }
    public int RemainingQuantity { get; set; }
    public int MaxPerCustomer { get; set; }
    public DateTime SaleStartTime { get; set; }
    public DateTime SaleEndTime { get; set; }

    // OData navigation, e.g. $expand=store($select=name,district).
    public StoreODataDto? Store { get; set; }
}
