using ClosedXML.Excel;
using FreshRescue.Business.Common;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Ordering;

public class OrderExportService : IOrderExportService
{
    private static readonly TimeSpan _vietnamOffset = TimeSpan.FromHours(7);

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly ILogger<OrderExportService> _logger;

    public OrderExportService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider clock,
        ILogger<OrderExportService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportBrandOrdersAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(AppRoles.BrandManager) || !_currentUser.BrandId.HasValue)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Chỉ Brand Manager mới có quyền export đơn của thương hiệu.");
        }

        var brandId = _currentUser.BrandId.Value;
        return await ExportOrdersInternalAsync(brandId, from, to, cancellationToken);
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportAdminOrdersAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(AppRoles.SystemAdmin))
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Chỉ System Admin mới có quyền export đơn toàn sàn.");
        }

        return await ExportOrdersInternalAsync(brandId: null, from, to, cancellationToken);
    }

    private async Task<(byte[] FileBytes, string FileName)> ExportOrdersInternalAsync(
        Guid? brandId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Khoảng ngày không hợp lệ: ngày bắt đầu phải trước ngày kết thúc.");
        }

        // BR-22, BR-27: truy vấn phải IgnoreQueryFilters để đơn của chi nhánh/mặt hàng đã xoá mềm vẫn có mặt.
        var query = _uow.Orders.Query()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.Items)
            .AsQueryable();

        if (brandId.HasValue)
        {
            query = query.Where(o => o.Store.BrandId == brandId.Value);
        }

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt < toUtc);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        // Lấy danh sách Store liên quan để lập bảng Summary (khớp hoàn toàn với GET /brand/stats theo BR-25).
        List<Store> stores;
        if (brandId.HasValue)
        {
            var activeStores = await _uow.Stores.Query()
                .AsNoTracking()
                .Where(s => s.BrandId == brandId.Value && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            var storeIdsWithOrders = orders.Select(o => o.StoreId).Distinct().ToHashSet();

            var deletedStoresWithOrders = await _uow.Stores.Query()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.BrandId == brandId.Value && s.IsDeleted && storeIdsWithOrders.Contains(s.Id))
                .ToListAsync(cancellationToken);

            stores = activeStores.Concat(deletedStoresWithOrders).OrderBy(s => s.Name).ToList();
        }
        else
        {
            stores = await _uow.Stores.Query()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);
        }

        var storeMap = stores.ToDictionary(s => s.Id);

        var fileBytes = GenerateExcelFile(orders, storeMap);
        var fileName = GenerateFileName(from, to);

        _logger.LogInformation(
            "Xuất Excel thành công: {OrderCount} đơn hàng, tệp {FileName}, BrandId={BrandId}",
            orders.Count, fileName, brandId);

        return (fileBytes, fileName);
    }

    private byte[] GenerateExcelFile(List<Order> orders, Dictionary<Guid, Store> storeMap)
    {
        using var workbook = new XLWorkbook();

        // 1. Sheet Orders (03-API-CONTRACT.md §7.1)
        var wsOrders = workbook.Worksheets.Add("Orders");

        string[] orderHeaders =
        [
            "OrderCode",
            "StoreName",
            "CreatedAt (UTC+7)",
            "Status",
            "RefundStatus",
            "TotalAmount",
            "PlatformFee",
            "StorePayoutAmount",
            "ItemCount"
        ];

        for (var col = 0; col < orderHeaders.Length; col++)
        {
            var cell = wsOrders.Cell(1, col + 1);
            cell.Value = orderHeaders[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var row = 2;
        foreach (var order in orders)
        {
            var storeName = order.Store?.Name
                ?? (storeMap.TryGetValue(order.StoreId, out var s) ? s.Name : string.Empty);

            var createdAtVn = DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc).Add(_vietnamOffset);
            var itemCount = order.Items.Sum(i => i.Quantity);

            wsOrders.Cell(row, 1).SetValue(order.OrderCode);
            wsOrders.Cell(row, 2).SetValue(storeName);
            wsOrders.Cell(row, 3).SetValue(createdAtVn.ToString("yyyy-MM-dd HH:mm:ss"));
            wsOrders.Cell(row, 4).SetValue(order.Status.ToString());
            wsOrders.Cell(row, 5).SetValue(order.RefundStatus.ToString());

            wsOrders.Cell(row, 6).SetValue(order.TotalAmount).Style.NumberFormat.Format = "#,##0";
            wsOrders.Cell(row, 7).SetValue(order.PlatformFee).Style.NumberFormat.Format = "#,##0";
            wsOrders.Cell(row, 8).SetValue(order.StorePayoutAmount).Style.NumberFormat.Format = "#,##0";
            wsOrders.Cell(row, 9).SetValue(itemCount).Style.NumberFormat.Format = "#,##0";

            row++;
        }

        wsOrders.Columns().AdjustToContents();

        // 2. Sheet Summary (BR-25: Dùng chung với GET /brand/stats qua BrandStatsCalculator)
        var wsSummary = workbook.Worksheets.Add("Summary");
        var stats = BrandStatsCalculator.Calculate(orders, storeMap.Values);

        // Tiêu đề tổng quan
        wsSummary.Cell(1, 1).SetValue("Chỉ số tổng hợp (BR-25)").Style.Font.Bold = true;
        wsSummary.Cell(1, 1).Style.Font.FontSize = 13;

        wsSummary.Cell(3, 1).SetValue("Tổng doanh thu (VNĐ):").Style.Font.Bold = true;
        wsSummary.Cell(3, 2).SetValue(stats.TotalRevenue).Style.NumberFormat.Format = "#,##0";

        wsSummary.Cell(4, 1).SetValue("Tổng số suất đã cứu:").Style.Font.Bold = true;
        wsSummary.Cell(4, 2).SetValue(stats.TotalRescuedPortions).Style.NumberFormat.Format = "#,##0";

        // Thống kê theo chi nhánh
        wsSummary.Cell(6, 1).SetValue("Thống kê theo chi nhánh").Style.Font.Bold = true;
        wsSummary.Cell(6, 1).Style.Font.FontSize = 12;

        string[] storeHeaders = ["StoreName", "Revenue", "RescuedPortions", "NoShowRate", "RatingAverage"];
        for (var col = 0; col < storeHeaders.Length; col++)
        {
            var cell = wsSummary.Cell(7, col + 1);
            cell.Value = storeHeaders[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var storeRow = 8;
        foreach (var storeStat in stats.ByStore)
        {
            wsSummary.Cell(storeRow, 1).SetValue(storeStat.StoreName);
            wsSummary.Cell(storeRow, 2).SetValue(storeStat.Revenue).Style.NumberFormat.Format = "#,##0";
            wsSummary.Cell(storeRow, 3).SetValue(storeStat.RescuedPortions).Style.NumberFormat.Format = "#,##0";
            wsSummary.Cell(storeRow, 4).SetValue(storeStat.NoShowRate).Style.NumberFormat.Format = "0.0%";
            wsSummary.Cell(storeRow, 5).SetValue((double)storeStat.RatingAverage).Style.NumberFormat.Format = "0.0";

            storeRow++;
        }

        wsSummary.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private string GenerateFileName(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue)
        {
            return $"orders_{from.Value:yyyy-MM-dd}_{to.Value:yyyy-MM-dd}.xlsx";
        }

        if (from.HasValue)
        {
            return $"orders_from_{from.Value:yyyy-MM-dd}.xlsx";
        }

        if (to.HasValue)
        {
            return $"orders_to_{to.Value:yyyy-MM-dd}.xlsx";
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        return $"orders_{now:yyyy-MM-dd}.xlsx";
    }
}
