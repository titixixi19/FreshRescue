namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>
/// Thống kê doanh thu và giải cứu thực phẩm của thương hiệu (FR-B06, BR-25, 03-API-CONTRACT.md §7).
/// </summary>
public record BrandStatsDto
{
    /// <summary>
    /// Tổng doanh thu (VNĐ): SUM(StorePayoutAmount) của các đơn Redeemed hoặc NoShow và RefundStatus=None (BR-25).
    /// </summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>
    /// Tổng số suất đã cứu: SUM(Quantity) của các đơn Redeemed (BR-25).
    /// </summary>
    public int TotalRescuedPortions { get; init; }

    /// <summary>
    /// Thống kê chi tiết theo từng chi nhánh.
    /// </summary>
    public IReadOnlyList<StoreStatsDto> ByStore { get; init; } = [];
}

/// <summary>
/// Thống kê của một chi nhánh thuộc thương hiệu.
/// </summary>
public record StoreStatsDto
{
    public Guid StoreId { get; init; }

    public string StoreName { get; init; } = string.Empty;

    public decimal Revenue { get; init; }

    public int RescuedPortions { get; init; }

    public double NoShowRate { get; init; }

    public decimal RatingAverage { get; init; }
}
