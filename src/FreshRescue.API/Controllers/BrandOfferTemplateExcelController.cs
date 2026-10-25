using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreshRescue.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.BrandManager)]
[Route("api/v1/brand/offer-templates")]
public sealed class BrandOfferTemplateExcelController : ControllerBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly IOfferTemplateExcelService _excelService;

    public BrandOfferTemplateExcelController(IOfferTemplateExcelService excelService)
    {
        _excelService = excelService;
    }

    [HttpGet("import-template")]
    public IActionResult DownloadTemplate()
    {
        return File(_excelService.CreateImportTemplate(), ExcelContentType,
            "FreshRescue_OfferTemplate_Import.xlsx");
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)] // 5 MB for file + multipart form overhead.
    public async Task<IActionResult> ImportAsync([FromForm] IFormFile? file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return ValidationError(new Dictionary<string, string[]>
            {
                ["file"] = ["Vui lòng chọn file .xlsx không rỗng."]
            });
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationError(new Dictionary<string, string[]>
            {
                ["file"] = ["Chỉ nhận file có đuôi .xlsx."]
            });
        }

        if (file.Length > OfferTemplateExcelService.MaxFileBytes)
        {
            return ValidationError(new Dictionary<string, string[]>
            {
                ["file"] = ["File không được vượt quá 5 MB."]
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await _excelService.ImportAsync(stream, ct);
        return result.IsValid
            ? Ok(new { importedCount = result.ImportedCount })
            : ValidationError(result.Errors);
    }

    private IActionResult ValidationError(Dictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Title = "Dữ liệu import không hợp lệ",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = ErrorCodes.ValidationFailed;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem);
    }
}
