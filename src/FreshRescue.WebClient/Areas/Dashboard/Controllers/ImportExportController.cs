using FreshRescue.WebClient.Models.Api;
using FreshRescue.WebClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FreshRescue.WebClient.Areas.Dashboard.Controllers;

/// <summary>
/// Import mẫu mặt hàng (FR-B07, BR-26) và export đơn hàng (FR-B08, FR-A07, BR-27).
/// </summary>
[Area("Dashboard")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.BrandManager)]
public class ImportExportController : Controller
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IApiClient _api;

    public ImportExportController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.BrandManager)]
    [ActionName("ImportTemplate")]
    public async Task<IActionResult> ImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = await _api.DownloadFileAsync("brand/offer-templates/import-template", cancellationToken);
            return File(stream, ExcelContentType, "FreshRescue_OfferTemplate_Import.xlsx");
        }
        catch (ApiException ex)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message) ? "Không tải được file mẫu." : ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.BrandManager)]
    [ActionName("Import")]
    public async Task<IActionResult> ImportAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file .xlsx.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _api.UploadFileAsync<ImportOfferTemplatesResponse>(
                "brand/offer-templates/import", file, "file", cancellationToken);
            TempData["Success"] = $"Đã import {result.ImportedCount} mẫu mặt hàng.";
        }
        catch (ApiException ex)
        {
            // BR-26: file lỗi thì không dòng nào được lưu, liệt kê từng ô sai để người dùng sửa file.
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message) ? "Import thất bại." : ex.Message;
            if (ex.Errors.Count > 0)
            {
                TempData["ImportErrors"] = string.Join("\n",
                    ex.Errors.Select(e => $"{e.Key}: {string.Join(" ", e.Value)}"));
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [ActionName("Export")]
    public async Task<IActionResult> ExportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>();
        if (from.HasValue)
        {
            query["from"] = from.Value.ToString("yyyy-MM-dd");
        }
        if (to.HasValue)
        {
            query["to"] = to.Value.ToString("yyyy-MM-dd");
        }

        // Admin xuất toàn sàn (FR-A07), Brand Manager chỉ xuất thương hiệu mình (FR-B08).
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var basePath = isAdmin ? "admin/orders/export" : "brand/orders/export";
        var fileName = (isAdmin ? "FreshRescue_DonHang_ToanSan" : "FreshRescue_DonHang_ThuongHieu")
            + $"_{from?.ToString("yyyyMMdd") ?? "dau"}_{to?.ToString("yyyyMMdd") ?? "nay"}.xlsx";

        try
        {
            var stream = await _api.DownloadFileAsync(QueryHelpers.AddQueryString(basePath, query), cancellationToken);
            return File(stream, ExcelContentType, fileName);
        }
        catch (ApiException ex)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message) ? "Export thất bại." : ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
