using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Catalog.Dtos;

namespace FreshRescue.Business.Features.Catalog;

public interface IOfferTemplateService
{
    Task<PagedResponse<OfferTemplateDto>> GetAllAsync(GetOfferTemplatesRequest request, CancellationToken ct);
    Task<OfferTemplateDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<OfferTemplateDto> CreateAsync(UpsertOfferTemplateRequest request, CancellationToken ct);
    Task<OfferTemplateDto> UpdateAsync(Guid id, UpsertOfferTemplateRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
