using FreshRescue.Business.Features.Ordering;

namespace FreshRescue.API.BackgroundServices;

/// <summary>
/// FR-X04: Tiến trình nền quét đơn Paid mà khách không tới nhận sau PickupEndTime,
/// đánh dấu NoShow (BR-19: không hoàn tiền).
/// Vòng lặp không chứa logic nghiệp vụ — uỷ thác qua IOrderService (07-WORK-BREAKDOWN.md §12.2).
/// </summary>
public class NoShowService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<NoShowService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public NoShowService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILogger<NoShowService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NoShowService bắt đầu khởi chạy (chu kỳ {IntervalSeconds}s).", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval, _clock);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNoShowsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp NoShowService.");
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

        _logger.LogInformation("NoShowService đã dừng.");
    }

    /// <summary>
    /// Thực hiện 1 chu kỳ quét đơn NoShow. Mở public để kiểm thử đơn vị độc lập với timer.
    /// </summary>
    public async Task ProcessNoShowsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var noShows = await orderService.ProcessNoShowsAsync(cancellationToken);
        if (noShows > 0)
        {
            _logger.LogInformation("NoShowService: Đã đánh dấu {NoShowCount} đơn NoShow.", noShows);
        }
    }
}
