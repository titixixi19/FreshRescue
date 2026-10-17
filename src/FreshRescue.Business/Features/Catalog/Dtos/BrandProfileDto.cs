using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Catalog.Dtos;

public record BrandProfileDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? LogoUrl { get; init; }

    public string? Description { get; init; }

    public BrandStatus Status { get; init; }

    public string? StatusReason { get; init; }

    public DateTime? ApprovedAt { get; init; }

    public DateTime CreatedAt { get; init; }
}
