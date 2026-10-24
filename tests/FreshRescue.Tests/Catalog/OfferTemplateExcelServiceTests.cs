using ClosedXML.Excel;
using FluentAssertions;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.Business.Features.Catalog.Validators;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreshRescue.Tests.Catalog;

public sealed class OfferTemplateExcelServiceTests : IDisposable
{
    private readonly FreshRescueDbContext _db;
    private readonly AppUser _owner;
    private readonly Brand _brand;
    private readonly Category _category;

    public OfferTemplateExcelServiceTests()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new FreshRescueDbContext(options, TimeProvider.System);
        _owner = new AppUser
        {
            FullName = "Import test manager",
            UserName = "import@example.test",
            Email = "import@example.test"
        };
        _brand = new Brand
        {
            Name = "Import Brand",
            Slug = "import-brand",
            OwnerUserId = _owner.Id,
            Owner = _owner,
            Status = BrandStatus.Approved
        };
        _category = new Category { Name = "Bánh mặn", Slug = "banh-man" };
        _db.Brands.Add(_brand);
        _db.Categories.Add(_category);
        _db.SaveChanges();
    }

    [Fact]
    public void CreateImportTemplate_ContainsRequiredNineHeaders()
    {
        var bytes = CreateService().CreateImportTemplate();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("OfferTemplates");

        for (var i = 0; i < OfferTemplateExcelService.Columns.Length; i++)
        {
            worksheet.Cell(1, i + 1).GetString()
                .Should().Be(OfferTemplateExcelService.Columns[i]);
        }
    }

    [Fact]
    public async Task ImportAsync_TwoValidRows_PersistsAllForOwnedBrandAsync()
    {
        using var stream = WorkbookWithRows(("SurpriseBag", 75m), ("ClearanceItem", 50m));

        var result = await CreateService().ImportAsync(stream, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.ImportedCount.Should().Be(2);
        var inserted = await _db.OfferTemplates.ToListAsync();
        inserted.Should().HaveCount(2);
        inserted.Should().OnlyContain(x => x.BrandId == _brand.Id && x.CategoryId == _category.Id);
    }

    [Fact]
    public async Task ImportAsync_OneInvalidRow_DoesNotInsertAnyRowsAsync()
    {
        using var stream = WorkbookWithRows(("SurpriseBag", 75m), ("SurpriseBag", 80m));

        var result = await CreateService().ImportAsync(stream, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("rows[3].brandMaxDiscountPercent");
        (await _db.OfferTemplates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_UnknownCategorySlug_ReportsExcelRowAsync()
    {
        using var stream = WorkbookWithCategory("category-not-found", ("SurpriseBag", 75m));

        var result = await CreateService().ImportAsync(stream, CancellationToken.None);

        result.Errors.Should().ContainKey("rows[2].categorySlug");
        (await _db.OfferTemplates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_InvalidOfferType_ReportsFieldAsync()
    {
        using var stream = WorkbookWithRows(("UnknownType", 75m));

        var result = await CreateService().ImportAsync(stream, CancellationToken.None);

        result.Errors.Should().ContainKey("rows[2].offerType");
        (await _db.OfferTemplates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_MoreThanTwoHundredRows_RejectsAllAsync()
    {
        using var stream = WorkbookWithRows(Enumerable.Repeat(("SurpriseBag", 75m), 201).ToArray());

        var result = await CreateService().ImportAsync(stream, CancellationToken.None);

        result.Errors.Should().ContainKey("file");
        (await _db.OfferTemplates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_CorruptWorkbook_RejectsFileAsync()
    {
        using var stream = new MemoryStream("not-excel"u8.ToArray());

        var result = await CreateService().ImportAsync(stream, CancellationToken.None);

        result.Errors.Should().ContainKey("file");
        (await _db.OfferTemplates.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_NoBrandClaim_RejectsAccessAsync()
    {
        using var stream = WorkbookWithRows(("SurpriseBag", 75m));
        var action = () => CreateService(hasBrandClaim: false)
            .ImportAsync(stream, CancellationToken.None);

        await action.Should().ThrowAsync<AppException>()
            .Where(ex => ex.ErrorCode == ErrorCodes.ForbiddenScope);
        (await _db.OfferTemplates.CountAsync()).Should().Be(0);
    }

    private IOfferTemplateExcelService CreateService(bool hasBrandClaim = true)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(x => x.UserId).Returns(_owner.Id);
        currentUser.Setup(x => x.BrandId).Returns(hasBrandClaim ? _brand.Id : null);
        return new OfferTemplateExcelService(new UnitOfWork(_db), currentUser.Object,
            new UpsertOfferTemplateRequestValidator());
    }

    private MemoryStream WorkbookWithRows(params (string type, decimal max)[] items)
        => WorkbookWithCategory(_category.Slug, items);

    private static MemoryStream WorkbookWithCategory(string categorySlug,
        params (string type, decimal max)[] items)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("OfferTemplates");
        for (var i = 0; i < OfferTemplateExcelService.Columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = OfferTemplateExcelService.Columns[i];
        }

        for (var i = 0; i < items.Length; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = $"Mẫu nhập {i + 1}";
            sheet.Cell(row, 2).Value = "Mô tả";
            sheet.Cell(row, 4).Value = categorySlug;
            sheet.Cell(row, 5).Value = items[i].type;
            sheet.Cell(row, 6).Value = 100000m;
            var surprise = items[i].type != "ClearanceItem";
            sheet.Cell(row, 7).Value = surprise ? 30000m : 55000m;
            sheet.Cell(row, 8).Value = surprise ? 65m : 40m;
            sheet.Cell(row, 9).Value = items[i].max;
        }

        var output = new MemoryStream();
        workbook.SaveAs(output);
        output.Position = 0;
        return output;
    }

    public void Dispose() => _db.Dispose();
}
