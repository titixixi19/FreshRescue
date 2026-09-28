namespace FreshRescue.Business.Common;

/// <summary>
/// Khuôn trả về của MỌI endpoint danh sách (03-API-CONTRACT.md §1.1, NFR-06).
/// </summary>
public record PagedResponse<T>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
