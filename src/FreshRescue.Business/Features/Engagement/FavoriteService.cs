using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Engagement;

public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IMapper _mapper;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider clock,
        IMapper mapper,
        ILogger<FavoriteService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> AddFavoriteAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var userId = RequireCustomerId();

        var storeExists = await _uow.Stores.Query()
            .AsNoTracking()
            .AnyAsync(s => s.Id == storeId && !s.IsDeleted, cancellationToken);

        if (!storeExists)
        {
            throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy cửa hàng.");
        }

        var existing = await _uow.FavoriteStores.Query()
            .SingleOrDefaultAsync(f => f.CustomerUserId == userId && f.StoreId == storeId, cancellationToken);

        if (existing is not null)
        {
            return true;
        }

        var favorite = new FavoriteStore
        {
            CustomerUserId = userId,
            StoreId = storeId,
            CreatedAt = _clock.GetUtcNow().UtcDateTime,
        };

        await _uow.FavoriteStores.AddAsync(favorite, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Khách hàng {UserId} đã thêm cửa hàng {StoreId} vào yêu thích (FR-C12).", userId, storeId);
        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var userId = RequireCustomerId();

        var existing = await _uow.FavoriteStores.Query()
            .SingleOrDefaultAsync(f => f.CustomerUserId == userId && f.StoreId == storeId, cancellationToken);

        if (existing is null)
        {
            return true;
        }

        _uow.FavoriteStores.Remove(existing);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Khách hàng {UserId} đã bỏ yêu thích cửa hàng {StoreId}.", userId, storeId);
        return true;
    }

    public Task<PagedResponse<FavoriteStoreDto>> GetMyFavoritesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var userId = RequireCustomerId();

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize is < 1 or > 100)
        {
            pageSize = 20;
        }

        var query = _uow.FavoriteStores.Query()
            .AsNoTracking()
            .Where(f => f.CustomerUserId == userId && !f.Store.IsDeleted)
            .OrderByDescending(f => f.CreatedAt);

        return query.ToPagedResponseAsync<FavoriteStore, FavoriteStoreDto>(page, pageSize, _mapper.ConfigurationProvider, cancellationToken);
    }

    private Guid RequireCustomerId()
    {
        return _currentUser.UserId ?? throw AppException.Unauthorized(ErrorCodes.Unauthenticated, "Vui lòng đăng nhập.");
    }
}
