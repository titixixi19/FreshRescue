using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

public sealed class GetOfferTemplatesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public OfferType? OfferType { get; set; }
}
