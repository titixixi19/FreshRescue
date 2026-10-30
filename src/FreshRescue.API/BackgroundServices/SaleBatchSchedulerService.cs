using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Sales;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.API.BackgroundServices;

/// <summary>
/// FR-X01, FR-X02: Tiến trình nền quét và chuyển trạng thái đợt bán.
/// Scheduled -> Active tại SaleStartTime; Active -> Closed tại PickupEndTime.
/// Không đổi trạng thái tại SaleEndTime (01 §6.5).
/// Phát sự kiện SignalR SaleBatchActivated/SaleBatchClosed và gửi thông báo cho khách yêu thích chi nhánh (OQ-29).
/// </summary>
public class SaleBatchSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<SaleBatchSchedulerService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public SaleBatchSchedulerService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILogger<SaleBatchSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SaleBatchSchedulerService bắt đầu khởi chạy (chu kỳ {IntervalSeconds}s).", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval, _clock);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp SaleBatchSchedulerService.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("SaleBatchSchedulerService đã dừng.");
    }

    /// <summary>
    /// Thực hiện 1 chu kỳ quét đợt bán. Mở public để kiểm thử đơn vị độc lập với timer.
    /// </summary>
    public async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var saleBatchService = scope.ServiceProvider.GetRequiredService<ISaleBatchService>();
        var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
        var realtimeNotifier = scope.ServiceProvider.GetService<IOfferRealtimeNotifier>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        var now = _clock.GetUtcNow().UtcDateTime;

        // 1. Thu thập thông tin đợt sắp kích hoạt và đợt sắp đóng trước khi đổi trạng thái (nếu có IUnitOfWork)
        var dueToActivate = new List<(Guid Id, Guid StoreId, string StoreName, int OfferCount)>();
        var dueToCloseIds = new List<Guid>();

        if (uow is not null)
        {
            try
            {
                var batchesToActivate = await uow.SaleBatches.Query()
                    .AsNoTracking()
                    .Include(b => b.Store)
                    .Include(b => b.Offers)
                    .Where(b => b.Status == SaleBatchStatus.Scheduled && b.SaleStartTime <= now)
                    .ToListAsync(cancellationToken);

                dueToActivate = batchesToActivate.Select(b => (
                    b.Id,
                    b.StoreId,
                    b.Store?.Name ?? "cửa hàng",
                    b.Offers?.Count(o => !o.IsDeleted) ?? 0
                )).ToList();

                dueToCloseIds = await uow.SaleBatches.Query()
                    .AsNoTracking()
                    .Where(b => b.Status == SaleBatchStatus.Active && b.PickupEndTime <= now)
                    .Select(b => b.Id)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải trước danh sách đợt bán để phát thông báo.");
            }
        }

        // 2. Kích hoạt đợt bán
        var activated = await saleBatchService.ActivateDueBatchesAsync(cancellationToken);
        if (activated > 0)
        {
            _logger.LogInformation("SaleBatchSchedulerService: Đã kích hoạt {ActivatedCount} đợt bán sang Active.", activated);

            foreach (var batch in dueToActivate)
            {
                if (realtimeNotifier is not null)
                {
                    try
                    {
                        await realtimeNotifier.NotifySaleBatchActivatedAsync(
                            batch.Id, batch.StoreId, batch.StoreName, batch.OfferCount, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi phát sự kiện SaleBatchActivated cho đợt {BatchId}.", batch.Id);
                    }
                }

                if (notificationService is not null)
                {
                    try
                    {
                        await notificationService.NotifyNewSaleBatchAsync(
                            batch.Id, batch.StoreId, batch.StoreName, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi gửi thông báo đợt bán mới cho người theo dõi chi nhánh {StoreId}.", batch.StoreId);
                    }
                }
            }
        }

        // 3. Đóng đợt bán
        var closed = await saleBatchService.CloseDueBatchesAsync(cancellationToken);
        if (closed > 0)
        {
            _logger.LogInformation("SaleBatchSchedulerService: Đã đóng {ClosedCount} đợt bán sang Closed.", closed);

            if (realtimeNotifier is not null)
            {
                foreach (var batchId in dueToCloseIds)
                {
                    try
                    {
                        await realtimeNotifier.NotifySaleBatchClosedAsync(batchId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi phát sự kiện SaleBatchClosed cho đợt {BatchId}.", batchId);
                    }
                }
            }
        }
    }
}
