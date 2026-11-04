using FreshRescue.WebClient.Models.Api;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

/// <summary>Một dòng của <c>GET admin/disputes</c>. Enum về dạng chuỗi (03 §1) — khai báo int là vỡ khi đọc JSON.</summary>
public class AdminDisputeDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderCode { get; set; }
    public string? CustomerName { get; set; }
    public DisputeReason Reason { get; set; }
    public string Description { get; set; } = default!;
    public string PhotoUrl { get; set; } = default!;

    /// <summary>Open · Approved · Rejected.</summary>
    public string Status { get; set; } = "Open";
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
