using FreshRescue.Business.Features.Sales;

namespace FreshRescue.API.BackgroundServices;

/// <summary>
/// FR-X01, FR-X02: Tiến trình nền quét và chuyển trạng thái đợt bán.
/// Scheduled -> Active tại SaleStartTime; Active -> Closed tại PickupEndTime.
/// Không đổi trạng thái tại SaleEndTime (01 §6.5).
/// Vòng lặp không chứa logic nghiệp vụ — uỷ thác qua ISaleBatchService (07-WORK-BREAKDOWN.md §12.2).
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

        var activated = await saleBatchService.ActivateDueBatchesAsync(cancellationToken);
        if (activated > 0)
        {
            _logger.LogInformation("SaleBatchSchedulerService: Đã kích hoạt {ActivatedCount} đợt bán sang Active.", activated);
        }

        var closed = await saleBatchService.CloseDueBatchesAsync(cancellationToken);
        if (closed > 0)
        {
            _logger.LogInformation("SaleBatchSchedulerService: Đã đóng {ClosedCount} đợt bán sang Closed.", closed);
        }
    }
}
