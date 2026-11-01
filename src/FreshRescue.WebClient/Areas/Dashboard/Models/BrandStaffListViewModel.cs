using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

public class BrandStaffListViewModel
{
    public IReadOnlyList<BrandStaffDto> Items { get; set; } = Array.Empty<BrandStaffDto>();
    public Guid? StoreIdFilter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }
}
