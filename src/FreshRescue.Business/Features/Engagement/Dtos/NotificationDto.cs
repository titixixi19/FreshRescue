using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Engagement.Dtos;

/// <summary>
/// DTO thông báo trong ứng dụng (FR-C13).
/// </summary>
public record NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Bộ lọc danh sách thông báo.
/// </summary>
public record NotificationListQuery
{
    public bool? UnreadOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
