using FreshRescue.Business.Common;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Ordering.Dtos;

/// <summary>Tham số query của <c>GET /me/orders</c> và <c>GET /staff/sale-batches/{id}/orders</c>.</summary>
public record OrderListQuery
{
    public OrderStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedResponse<object>.DefaultPageSize;
}
