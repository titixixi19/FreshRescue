using FreshRescue.Business.Features.Ordering;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FreshRescue.Business.Features.Sales;

public class SaleBatchService : ISaleBatchService
{
    private readonly IUnitOfWork _uow;
    private readonly IOrderService _orderService;
    private readonly TimeProvider _clock;
    private readonly ILogger<SaleBatchService> _logger;

    public SaleBatchService(
        IUnitOfWork uow,
        IOrderService orderService,
        TimeProvider clock,
        ILogger<SaleBatchService> logger)
    {
        _uow = uow;
        _orderService = orderService;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// FR-X01: Quét các đợt bán Scheduled đã tới SaleStartTime và chuyển sang Active.
    /// </summary>
    public async Task<int> ActivateDueBatchesAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var dueBatches = await _uow.SaleBatches.Query()
            .Where(b => b.Status == SaleBatchStatus.Scheduled && b.SaleStartTime <= now)
            .ToListAsync(cancellationToken);

        if (dueBatches.Count == 0)
        {
            return 0;
        }

        foreach (var batch in dueBatches)
        {
            batch.Status = SaleBatchStatus.Active;
            batch.ActivatedAt = now;
            batch.UpdatedAt = now;

            _logger.LogInformation(
                "Kích hoạt đợt bán {SaleBatchId} ('{BatchName}') tại {Now}. SaleStartTime={SaleStartTime}",
                batch.Id, batch.Name, now, batch.SaleStartTime);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã kích hoạt {Count} đợt bán sang Active (FR-X01).", dueBatches.Count);
        return dueBatches.Count;
    }

    /// <summary>
    /// FR-X02: Quét các đợt bán Active đã tới PickupEndTime và chuyển sang Closed.
    /// Lưu ý: Không đổi trạng thái tại SaleEndTime (01 §6.5).
    /// Trước khi đóng đợt, bắt buộc chạy đánh dấu NoShow cho các đơn của đợt (01 §6.5).
    /// Chốt TotalUnsoldQuantity và UnsoldQuantity cho từng Offer (OQ-15).
    /// </summary>
    public async Task<int> CloseDueBatchesAsync(CancellationToken cancellationToken)
    {
        // 01-REQUIREMENTS.md §6.5: FR-X04 (đánh dấu NoShow) phải chạy trước khi đóng đợt
        await _orderService.ProcessNoShowsAsync(cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;

        var dueBatches = await _uow.SaleBatches.Query()
            .Include(b => b.Offers)
            .Where(b => b.Status == SaleBatchStatus.Active && b.PickupEndTime <= now)
            .ToListAsync(cancellationToken);

        if (dueBatches.Count == 0)
        {
            return 0;
        }

        foreach (var batch in dueBatches)
        {
            batch.Status = SaleBatchStatus.Closed;
            batch.ClosedAt = now;
            batch.UpdatedAt = now;

            var totalUnsold = 0;
            foreach (var offer in batch.Offers)
            {
                offer.UnsoldQuantity = offer.RemainingQuantity;
                offer.UpdatedAt = now;
                totalUnsold += offer.RemainingQuantity;
            }

            batch.TotalUnsoldQuantity = totalUnsold;

            _logger.LogInformation(
                "Đóng đợt bán {SaleBatchId} ('{BatchName}') tại {Now}. PickupEndTime={PickupEndTime}, TotalUnsoldQuantity={TotalUnsold}",
                batch.Id, batch.Name, now, batch.PickupEndTime, totalUnsold);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã đóng {Count} đợt bán sang Closed (FR-X02).", dueBatches.Count);
        return dueBatches.Count;
    }
}
