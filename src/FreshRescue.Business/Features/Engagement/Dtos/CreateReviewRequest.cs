namespace FreshRescue.Business.Features.Engagement.Dtos;

public sealed record CreateReviewRequest(byte Rating, string? Comment, string? PhotoUrl);
