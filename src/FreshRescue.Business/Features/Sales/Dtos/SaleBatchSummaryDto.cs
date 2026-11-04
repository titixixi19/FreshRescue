using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>Dòng của danh sách đợt bán — không kèm offer để nhẹ.</summary>
public record SaleBatchSummaryDto
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

    public int OfferCount { get; init; }

    public DateTime CreatedAt { get; init; }
}
