using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Admin.Dtos;

public record AdminBrandDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Description { get; init; }

    public Guid OwnerUserId { get; init; }
    public string? OwnerEmail { get; init; }

    public BrandStatus Status { get; init; }
    public string? StatusReason { get; init; }

    public DateTime? ApprovedAt { get; init; }
    public Guid? ApprovedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
