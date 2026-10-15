using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Sales.Dtos;

public record OfferDto
{
    public Guid Id { get; init; }

    public Guid SaleBatchId { get; init; }

    public Guid OfferTemplateId { get; init; }

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public Guid CategoryId { get; init; }

    public OfferType OfferType { get; init; }

    public decimal OriginalPrice { get; init; }

    public decimal ActualPrice { get; init; }

    public int TotalQuantity { get; init; }

    public int RemainingQuantity { get; init; }

    public int MaxPerCustomer { get; init; }

    public int? UnsoldQuantity { get; init; }
}
