namespace FreshRescue.Business.Features.Catalog.Dtos;

public record CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public int DisplayOrder { get; init; }
}
