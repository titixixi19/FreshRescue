using FreshRescue.Business.Common;

namespace FreshRescue.Business.Features.Catalog.Dtos;

/// <summary>
/// Tham số truy vấn danh sách nhân viên chi nhánh của thương hiệu (03-API-CONTRACT.md §7).
/// </summary>
public record GetBrandStaffRequest
{
    public Guid? StoreId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedResponse<object>.DefaultPageSize;
}
