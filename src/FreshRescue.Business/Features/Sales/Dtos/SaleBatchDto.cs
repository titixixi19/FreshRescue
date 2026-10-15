using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Sales.Dtos;

public record SaleBatchDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public string StoreName { get; init; } = default!;

    public string? Name { get; init; }

    public SaleBatchStatus Status { get; init; }

    public DateTime SaleStartTime { get; init; }

    public DateTime SaleEndTime { get; init; }

    public DateTime PickupStartTime { get; init; }

    public DateTime PickupEndTime { get; init; }

    public DateTime? ActivatedAt { get; init; }

    public DateTime? ClosedAt { get; init; }

    public int? TotalUnsoldQuantity { get; init; }

    public string? CancelReason { get; init; }

    public List<OfferDto> Offers { get; init; } = [];

    public DateTime CreatedAt { get; init; }
}
