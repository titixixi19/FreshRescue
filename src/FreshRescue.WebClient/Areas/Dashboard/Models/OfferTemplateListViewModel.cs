namespace FreshRescue.WebClient.Areas.Dashboard.Models;

public class OfferTemplateListViewModel
{
    public IReadOnlyList<OfferTemplateDto> Items { get; set; } = Array.Empty<OfferTemplateDto>();
    public string? SearchQuery { get; set; }
    public Guid? CategoryId { get; set; }
    public int? OfferType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }
}
