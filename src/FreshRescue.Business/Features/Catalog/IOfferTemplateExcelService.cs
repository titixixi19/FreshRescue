using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

public interface IOfferTemplateExcelService
{
    byte[] CreateImportTemplate();

    Task<ImportOfferTemplatesResult> ImportAsync(Stream stream, CancellationToken ct);
}
