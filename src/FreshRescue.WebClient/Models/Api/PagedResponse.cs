namespace FreshRescue.WebClient.Models.Api;

/// <summary>Khuôn mọi danh sách của API (03-API-CONTRACT.md §1.1).</summary>
public record PagedResponse<T>
{
    public const int MaxPageSize = 100;

    public List<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
