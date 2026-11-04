using FreshRescue.Business.Features.Ordering;

namespace FreshRescue.API.BackgroundServices;

/// <summary>
/// FR-X03: Tiến trình nền quét đơn PendingPayment quá hạn PaymentDeadline,
/// huỷ đơn và hoàn tồn kho có chốt chặn (02 §6.4). Chạy định kỳ mỗi phút.
/// Vòng lặp không chứa logic nghiệp vụ — uỷ thác qua IOrderService (07-WORK-BREAKDOWN.md §12.2).
/// </summary>
public class OrderExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<OrderExpiryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public OrderExpiryService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILogger<OrderExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpiryService bắt đầu khởi chạy (chu kỳ {IntervalSeconds}s).", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval, _clock);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp OrderExpiryService.");
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

        _logger.LogInformation("OrderExpiryService đã dừng.");
    }

    /// <summary>
    /// Thực hiện 1 chu kỳ quét đơn quá hạn. Mở public để kiểm thử đơn vị độc lập với timer.
    /// </summary>
    public async Task ProcessExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var expired = await orderService.ExpireOverdueOrdersAsync(cancellationToken);
        if (expired > 0)
        {
            _logger.LogInformation("OrderExpiryService: Đã huỷ và hoàn tồn kho {ExpiredCount} đơn quá hạn.", expired);
        }
    }
}
