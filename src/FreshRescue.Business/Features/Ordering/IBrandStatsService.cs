using FreshRescue.Business.Features.Ordering.Dtos;

namespace FreshRescue.Business.Features.Ordering;

/// <summary>
/// Dịch vụ thống kê thương hiệu (FR-B06, BR-25, 03-API-CONTRACT.md §7).
/// </summary>
public interface IBrandStatsService
{
    /// <summary>
    /// Lấy thống kê doanh thu và cứu thực phẩm của thương hiệu theo khoảng ngày (FR-B06, BR-25).
    /// </summary>
    Task<BrandStatsDto> GetBrandStatsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);
}
