using AutoMapper;
using FluentValidation;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.DataAccess.Repositories;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Business.Features.Engagement;

/// <summary>
/// BR-12: only the order owner can review a Redeemed order once.
/// Review insert and atomic Store rating update share a SQL transaction.
/// </summary>
public sealed class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;
    private readonly IStoreRatingRepository _ratingRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateReviewRequest> _validator;
    private readonly IMapper _mapper;

    public ReviewService(
        IUnitOfWork uow,
        IStoreRatingRepository ratingRepository,
        ICurrentUser currentUser,
        IValidator<CreateReviewRequest> validator,
        IMapper mapper)
    {
        _uow = uow;
        _ratingRepository = ratingRepository;
        _currentUser = currentUser;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<ReviewDto> CreateAsync(
        Guid orderId,
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw AppException.BadRequest(
                ErrorCodes.ValidationFailed,
                string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)));
        }

        if (_currentUser.UserId is not Guid customerId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Bạn cần đăng nhập để đánh giá.");
        }

        var order = await _uow.Orders.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken) ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");
        if (order.CustomerUserId != customerId)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Bạn không sở hữu đơn hàng này.");
        }

        if (order.Status != OrderStatus.Redeemed)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Chỉ đơn đã nhận hàng mới được đánh giá.");
        }

        if (await _uow.Reviews.Query().AnyAsync(r => r.OrderId == orderId, cancellationToken))
        {
            throw AppException.Conflict(ErrorCodes.ReviewAlreadyExists, "Đơn hàng này đã được đánh giá.");
        }

        var review = new Review
        {
            OrderId = order.Id,
            CustomerUserId = customerId,
            StoreId = order.StoreId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim(),
            PhotoUrl = request.PhotoUrl?.Trim(),
        };

        // The Review.OrderId unique constraint is the final concurrency guard.
        // The transaction also guarantees that a failed rating update rolls back the review.
        await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

        try
        {
            await _uow.Reviews.AddAsync(review, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            var affected = await _ratingRepository.AddRatingAsync(
                order.StoreId, request.Rating, cancellationToken);

            if (affected != 1)
            {
                throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy cửa hàng để cập nhật điểm.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Concurrent requests can both pass AnyAsync. Database UNIQUE(OrderId)
            // ensures exactly one review; do not turn this into a generic HTTP 500.
            throw AppException.Conflict(ErrorCodes.ReviewAlreadyExists, "Đơn hàng này đã được đánh giá.");
        }

        return _mapper.Map<ReviewDto>(review);
    }

    public async Task<PagedResponse<ReviewDto>> GetStoreReviewsAsync(
        Guid storeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize < 1 || pageSize > PagedResponse<ReviewDto>.MaxPageSize)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Phân trang không hợp lệ (page >= 1, pageSize 1–100).");
        }

        var exists = await _uow.Stores.Query()
            .AsNoTracking()
            .AnyAsync(s => s.Id == storeId, cancellationToken);

        if (!exists)
        {
            throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy cửa hàng.");
        }

        var query = _uow.Reviews.Query()
            .AsNoTracking()
            .Where(r => r.StoreId == storeId);

        var count = await query.CountAsync(cancellationToken);
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(reviews),
            Page = page,
            PageSize = pageSize,
            TotalCount = count,
        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627;
}
