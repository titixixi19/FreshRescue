using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Ordering;

public class BrandStatsService : IBrandStatsService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<BrandStatsService> _logger;

    public BrandStatsService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<BrandStatsService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BrandStatsDto> GetBrandStatsAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(AppRoles.BrandManager) || !_currentUser.BrandId.HasValue)
        {
            throw AppException.Forbidden(ErrorCodes.ForbiddenScope, "Chỉ Brand Manager mới có quyền xem thống kê của thương hiệu.");
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationFailed, "Khoảng ngày không hợp lệ: ngày bắt đầu phải trước ngày kết thúc.");
        }

        var brandId = _currentUser.BrandId.Value;

        // BR-22, BR-27: truy vấn phải IgnoreQueryFilters để đơn của chi nhánh/mặt hàng đã xoá mềm vẫn có mặt.
        var query = _uow.Orders.Query()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.Items)
            .Where(o => o.Store.BrandId == brandId);

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt < toUtc);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        // byStore gồm mọi chi nhánh của brand (kể cả chưa có đơn) và chi nhánh đã xoá mềm nhưng có đơn trong khoảng (BR-22).
        var activeStores = await _uow.Stores.Query()
            .AsNoTracking()
            .Where(s => s.BrandId == brandId && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var storeIdsWithOrders = orders.Select(o => o.StoreId).Distinct().ToHashSet();

        var deletedStoresWithOrders = await _uow.Stores.Query()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.BrandId == brandId && s.IsDeleted && storeIdsWithOrders.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var allRelevantStores = activeStores
            .Concat(deletedStoresWithOrders)
            .OrderBy(s => s.Name)
            .ToList();

        var stats = BrandStatsCalculator.Calculate(orders, allRelevantStores);

        _logger.LogInformation(
            "Lấy thống kê thương hiệu thành công: BrandId={BrandId}, TotalRevenue={Revenue}, RescuedPortions={Rescued}, StoreCount={StoreCount}",
            brandId, stats.TotalRevenue, stats.TotalRescuedPortions, stats.ByStore.Count);

        return stats;
    }
}
