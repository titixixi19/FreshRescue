using FreshRescue.Business.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Sales.Dtos;

/// <summary>Tham số query của <c>GET /staff/sale-batches</c>. <c>Date</c> là ngày UTC của SaleStartTime (yyyy-MM-dd).</summary>
public record SaleBatchListQuery
{
    public SaleBatchStatus? Status { get; init; }

    public DateOnly? Date { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedResponse<object>.DefaultPageSize;
}
