namespace FreshRescue.Business.Features.Engagement.Dtos;

public sealed record ReviewDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public byte Rating { get; init; }

    public string? Comment { get; init; }

    public string? PhotoUrl { get; init; }

    public DateTime CreatedAt { get; init; }
}
