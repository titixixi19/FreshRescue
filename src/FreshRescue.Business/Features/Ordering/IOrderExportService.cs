namespace FreshRescue.Business.Features.Ordering;

/// <summary>
/// Xuất danh sách đơn hàng và thống kê ra file Excel (FR-B08, FR-A07, BR-27).
/// </summary>
public interface IOrderExportService
{
    /// <summary>
    /// Xuất đơn hàng của thương hiệu (FR-B08, BR-27: Brand Manager chỉ thấy chi nhánh của mình).
    /// </summary>
    Task<(byte[] FileBytes, string FileName)> ExportBrandOrdersAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken);

    /// <summary>
    /// Xuất đơn hàng toàn sàn (FR-A07, BR-27: Admin thấy toàn sàn).
    /// </summary>
    Task<(byte[] FileBytes, string FileName)> ExportAdminOrdersAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken);
}
