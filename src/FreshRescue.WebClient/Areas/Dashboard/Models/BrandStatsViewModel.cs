using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

public class BrandStatsViewModel
{
    public BrandStatsDto? Stats { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ErrorMessage { get; set; }
}
