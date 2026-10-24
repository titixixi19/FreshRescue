namespace FreshRescue.Business.Features.Catalog.Dtos;

public sealed class ImportOfferTemplatesResult
{
    public int ImportedCount { get; init; }

    // Keys follow the API contract: rows[4].originalPrice, rows[9].brandMaxDiscountPercent.
    public Dictionary<string, string[]> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}
