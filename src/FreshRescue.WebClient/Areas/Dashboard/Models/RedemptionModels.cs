using System.ComponentModel.DataAnnotations;

namespace FreshRescue.WebClient.Areas.Dashboard.Models;

/// <summary>
/// POST staff/redemptions (03-API-CONTRACT §6.3).
/// Staff quét QR hoặc nhập orderCode thủ công.
/// </summary>
public class RedeemOrderRequest
{
    /// <summary>Mã đơn hiển thị / quét được từ QR ticket.</summary>
    public string? OrderCode { get; set; }

    /// <summary>Token QR nếu contract/API chấp nhận thay cho orderCode.</summary>
    public string? QrToken { get; set; }
}

public class RedeemOrderResponse
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? RedeemedAt { get; set; }
}

public class RedemptionPageViewModel
{
    [Display(Name = "Mã đơn / nội dung QR")]
    [StringLength(200)]
    public string? ManualCode { get; set; }

    public string? LastResultMessage { get; set; }
    public bool? LastSucceeded { get; set; }
    public string? LastOrderCode { get; set; }
    public string? LastStatus { get; set; }
}
