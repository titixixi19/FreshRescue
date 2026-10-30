using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Engagement.Dtos;

public sealed record CreateDisputeRequest(
    DisputeReason Reason,
    string Description,
    string PhotoUrl);

public sealed record RejectDisputeRequest(string AdminNote);

public sealed record MerchantNoteRequest(string MerchantNote);

public sealed record DisputeDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerUserId,
    DisputeReason Reason,
    string Description,
    string PhotoUrl,
    DisputeStatus Status,
    string? MerchantNote,
    string? AdminNote,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAt,
    DateTime CreatedAt);
