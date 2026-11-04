namespace FreshRescue.WebClient.Areas.Dashboard.Models;

public class AdminDisputeDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public int Reason { get; set; }
    public string Description { get; set; } = default!;
    public string PhotoUrl { get; set; } = default!;
    public int Status { get; set; } // 0: Open, 1: Approved, 2: Rejected
    public string? MerchantNote { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DisputeListViewModel
{
    public List<AdminDisputeDto> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int? StatusFilter { get; set; }
    public string? ErrorMessage { get; set; }
}
