using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;

namespace FreshRescue.Business.Features.Engagement;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(Guid orderId, CreateReviewRequest request, CancellationToken cancellationToken);

    Task<PagedResponse<ReviewDto>> GetStoreReviewsAsync(
        Guid storeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
