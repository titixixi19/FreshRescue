using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Common;

public static class PagingExtensions
{
    /// <summary>
    /// Đếm, cắt trang rồi chiếu sang DTO ngay trong câu SQL (03-API-CONTRACT.md §1.1). Truy vấn
    /// đầu vào phải đã sắp xếp — không có ORDER BY thì SQL Server không bảo đảm trang ổn định.
    /// </summary>
    public static async Task<PagedResponse<TDto>> ToPagedResponseAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        int page,
        int pageSize,
        IConfigurationProvider mapperConfiguration,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<TDto>(mapperConfiguration)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
