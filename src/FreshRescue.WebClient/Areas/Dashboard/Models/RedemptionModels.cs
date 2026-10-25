using System.ComponentModel.DataAnnotations;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

/// <summary>
/// POST staff/redemptions (03-API-CONTRACT §6.3).
/// Body chỉ một trong hai: token (QR camera) hoặc orderCode (nhập tay).
/// </summary>
public class RedeemOrderRequest
{
    /// <summary>Payload QR do camera đọc — JSON: "token".</summary>
    public string? Token { get; set; }

    /// <summary>Mã đơn khi khách hết pin / nhập tay — JSON: "orderCode".</summary>
    public string? OrderCode { get; set; }
}

public class RedeemOrderResponse
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? RedeemedAt { get; set; }
}

public class RedemptionPageViewModel
{
    [Display(Name = "Mã đơn")]
    [StringLength(200)]
    public string? ManualCode { get; set; }

    public string? LastResultMessage { get; set; }
    public bool? LastSucceeded { get; set; }
    public string? LastOrderCode { get; set; }
    public string? LastStatus { get; set; }
    public string? LastCustomerName { get; set; }
}
