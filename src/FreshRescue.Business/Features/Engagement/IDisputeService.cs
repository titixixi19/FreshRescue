using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Entities.Enums;

namespace FreshRescue.Business.Features.Engagement;

public interface IDisputeService
{
    Task<DisputeDto> CreateAsync(Guid orderId, CreateDisputeRequest request, CancellationToken ct);

    Task<PagedResponse<DisputeDto>> GetMineAsync(int page, int pageSize, CancellationToken ct);

    Task<PagedResponse<DisputeDto>> GetAdminAsync(DisputeStatus? status, int page, int pageSize, CancellationToken ct);

    Task<DisputeDto> ApproveAsync(Guid disputeId, CancellationToken ct);

    Task<DisputeDto> RejectAsync(Guid disputeId, RejectDisputeRequest request, CancellationToken ct);

    Task<DisputeDto> SetMerchantNoteAsync(Guid disputeId, MerchantNoteRequest request, CancellationToken ct);
}
