using FreshRescue.Business.Features.Ordering;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public class AdminOrdersController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IOrderExportService _orderExportService;

    public AdminOrdersController(IOrderExportService orderExportService)
    {
        _orderExportService = orderExportService;
    }

    /// <summary>
    /// Xuất danh sách đơn hàng và thống kê toàn sàn ra file Excel (FR-A07, BR-27).
    /// </summary>
    /// <param name="from">Ngày bắt đầu (yyyy-MM-dd)</param>
    /// <param name="to">Ngày kết thúc (yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportOrdersAsync(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var (fileBytes, fileName) = await _orderExportService.ExportAdminOrdersAsync(from, to, cancellationToken);
        return File(fileBytes, ExcelContentType, fileName);
    }
}
