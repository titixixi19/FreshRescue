namespace FreshRescue.Business.Features.Catalog.Dtos;

public record UpdateBrandProfileRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? LogoUrl { get; init; }

    public string? Description { get; init; }
}
