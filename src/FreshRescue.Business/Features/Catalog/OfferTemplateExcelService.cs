using System.Globalization;
using ClosedXML.Excel;
using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Catalog;

/// <summary>
/// FR-B07, BR-26: validate the complete workbook before attaching any new entity;
/// one SaveChangesAsync persists all rows atomically.
/// </summary>
public sealed class OfferTemplateExcelService : IOfferTemplateExcelService
{
    public const long MaxFileBytes = 5L * 1024 * 1024;
    public const int MaxDataRows = 200;

    public static readonly string[] Columns =
    [
        "name", "description", "imageUrl", "categorySlug", "offerType",
        "originalPrice", "suggestedPrice", "brandMinDiscountPercent", "brandMaxDiscountPercent"
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<UpsertOfferTemplateRequest> _validator;

    public OfferTemplateExcelService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<UpsertOfferTemplateRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public byte[] CreateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("OfferTemplates");
        for (var col = 1; col <= Columns.Length; col++)
        {
            sheet.Cell(1, col).Value = Columns[col - 1];
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns(1, Columns.Length).AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        var instructions = workbook.Worksheets.Add("Instructions");
        instructions.Cell("A1").Value = "Điền dữ liệu tại sheet OfferTemplates, bắt đầu từ dòng 2.";
        instructions.Cell("A2").Value = "categorySlug: sử dụng slug của Category hiện có trong FreshRescue.";
        instructions.Cell("A3").Value = "offerType: SurpriseBag hoặc ClearanceItem.";
        instructions.Cell("A4").Value = "Giới hạn: file .xlsx <= 5 MB và tối đa 200 dòng dữ liệu.";
        instructions.Cell("A5").Value = "BR-26: Nếu một dòng sai, hệ thống không lưu bất kỳ dòng nào.";
        instructions.Column(1).Width = 93;

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public async Task<ImportOfferTemplatesResult> ImportAsync(Stream stream, CancellationToken ct)
    {
        if (stream is null || !stream.CanRead)
        {
            return FileError("Không thể đọc file Excel.");
        }

        if (stream.CanSeek && stream.Length > MaxFileBytes)
        {
            return FileError("File Excel không được vượt quá 5 MB.");
        }

        // Check authorization before using any data from the uploaded workbook.
        var brandId = await GetOwnedBrandIdAsync(ct);
        await BrandApproval.EnsureApprovedAsync(_unitOfWork, brandId, ct);

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            return FileError("File không phải workbook .xlsx hợp lệ hoặc đã bị hỏng.");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null || !HasValidHeader(sheet))
            {
                return FileError("Dòng tiêu đề Excel không đúng file mẫu (9 cột theo đúng thứ tự).");
            }

            var rows = sheet.RowsUsed(XLCellsUsedOptions.Contents)
                .Where(x => x.RowNumber() > 1).ToList();

            if (rows.Count == 0)
            {
                return FileError("File Excel không có dòng dữ liệu để nhập.");
            }

            if (rows.Count > MaxDataRows)
            {
                return FileError("Mỗi lần chỉ được nhập tối đa 200 dòng dữ liệu.");
            }

            var categories = await _unitOfWork.Categories.Query().AsNoTracking()
                .Select(x => new { x.Id, x.Slug }).ToListAsync(ct);
            var slugToId = categories.ToDictionary(x => x.Slug, x => x.Id,
                StringComparer.OrdinalIgnoreCase);

            var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var entities = new List<OfferTemplate>(rows.Count);

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                var excelRow = row.RowNumber();
                var name = row.Cell(1).GetString().Trim();
                var description = NullIfEmpty(row.Cell(2).GetString());
                var imageUrl = NullIfEmpty(row.Cell(3).GetString());
                var categorySlug = row.Cell(4).GetString().Trim();
                var offerTypeText = row.Cell(5).GetString().Trim();

                var categoryId = slugToId.TryGetValue(categorySlug, out var id) ? id : Guid.Empty;
                if (categoryId == Guid.Empty)
                {
                    AddError(errors, excelRow, "categorySlug", "Danh mục không tồn tại hoặc đã bị xóa.");
                }

                var validOfferTypeName =
                    string.Equals(offerTypeText, nameof(OfferType.SurpriseBag), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(offerTypeText, nameof(OfferType.ClearanceItem), StringComparison.OrdinalIgnoreCase);
                var parsedOfferType = Enum.TryParse<OfferType>(offerTypeText,
                    ignoreCase: true, out var offerType);
                if (!validOfferTypeName || !parsedOfferType)
                {
                    AddError(errors, excelRow, "offerType", "Chỉ nhận SurpriseBag hoặc ClearanceItem.");
                    offerType = default;
                }

                var originalPrice = ReadDecimal(row.Cell(6), errors, excelRow, "originalPrice");
                var suggestedPrice = ReadDecimal(row.Cell(7), errors, excelRow, "suggestedPrice");
                var minPercent = ReadDecimal(row.Cell(8), errors, excelRow, "brandMinDiscountPercent");
                var maxPercent = ReadDecimal(row.Cell(9), errors, excelRow, "brandMaxDiscountPercent");

                var request = new UpsertOfferTemplateRequest
                {
                    Name = name,
                    Description = description,
                    ImageUrl = imageUrl,
                    CategoryId = categoryId,
                    OfferType = offerType,
                    OriginalPrice = originalPrice,
                    SuggestedPrice = suggestedPrice,
                    BrandMinDiscountPercent = minPercent,
                    BrandMaxDiscountPercent = maxPercent
                };

                var validation = await _validator.ValidateAsync(request, ct);
                foreach (var failure in validation.Errors)
                {
                    // categorySlug is the Excel input equivalent of CategoryId.
                    var field = failure.PropertyName == nameof(request.CategoryId)
                        ? "categorySlug" : ToCamelCase(failure.PropertyName);
                    AddError(errors, excelRow, field, failure.ErrorMessage);
                }

                if (Enum.IsDefined(offerType) && originalPrice > 0m && suggestedPrice > 0m)
                {
                    try
                    {
                        OfferTemplateDiscountPolicy.Validate(
                            offerType, originalPrice, suggestedPrice, minPercent, maxPercent);
                    }
                    catch (AppException ex)
                    {
                        var field = ex.ErrorCode == ErrorCodes.PriceOutOfBand
                            && ex.Message.StartsWith("Giá gợi ý", StringComparison.Ordinal)
                            ? "suggestedPrice" : "brandMaxDiscountPercent";
                        AddError(errors, excelRow, field, ex.Message);
                    }
                }

                entities.Add(new OfferTemplate
                {
                    BrandId = brandId,
                    CategoryId = categoryId,
                    Name = name,
                    Description = description,
                    ImageUrl = imageUrl,
                    OfferType = offerType,
                    OriginalPrice = originalPrice,
                    SuggestedPrice = suggestedPrice,
                    BrandMinDiscountPercent = minPercent,
                    BrandMaxDiscountPercent = maxPercent
                });
            }

            if (errors.Count > 0)
            {
                return new ImportOfferTemplatesResult
                {
                    Errors = errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray())
                };
            }

            // No per-row SaveChanges: EF Core groups all inserts into one atomic SaveChanges.
            foreach (var entity in entities)
            {
                await _unitOfWork.OfferTemplates.AddAsync(entity, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new ImportOfferTemplatesResult { ImportedCount = entities.Count };
        }
    }

    private async Task<Guid> GetOwnedBrandIdAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId || _currentUser.BrandId is not Guid brandId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope,
                "Bạn không có quyền import mẫu hàng của thương hiệu.");
        }

        var owned = await _unitOfWork.Brands.Query().AsNoTracking()
            .AnyAsync(x => x.Id == brandId && x.OwnerUserId == userId, ct);
        if (!owned)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope,
                "Không được import vào thương hiệu khác.");
        }

        return brandId;
    }

    private static bool HasValidHeader(IXLWorksheet sheet)
    {
        for (var col = 1; col <= Columns.Length; col++)
        {
            if (!string.Equals(sheet.Cell(1, col).GetString().Trim(),
                    Columns[col - 1], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string? NullIfEmpty(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static decimal ReadDecimal(IXLCell cell, Dictionary<string, List<string>> errors,
        int row, string field)
    {
        if (cell.TryGetValue<decimal>(out var value))
        {
            return value;
        }

        var text = cell.GetString().Trim();
        if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        AddError(errors, row, field, "Phải là số hợp lệ (ví dụ: 120000 hoặc 65.5).");
        return 0m;
    }

    private static void AddError(Dictionary<string, List<string>> errors, int row,
        string field, string message)
    {
        var key = $"rows[{row}].{field}";
        if (!errors.TryGetValue(key, out var values))
        {
            values = [];
            errors[key] = values;
        }

        if (!values.Contains(message, StringComparer.Ordinal))
        {
            values.Add(message);
        }
    }

    private static string ToCamelCase(string field) => field.Length == 0
        ? field : char.ToLowerInvariant(field[0]) + field[1..];

    private static ImportOfferTemplatesResult FileError(string message) => new()
    {
        Errors = new Dictionary<string, string[]> { ["file"] = [message] }
    };
}
