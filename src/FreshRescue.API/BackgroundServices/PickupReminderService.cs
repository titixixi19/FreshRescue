using FreshRescue.Business.Features.Engagement;

namespace FreshRescue.API.BackgroundServices;

/// <summary>
/// FR-X05: Tiến trình nền quét đơn hàng Paid sắp hết hạn nhận hàng trong 30 phút,
/// tạo thông báo nhắc nhở khách hàng (07-WORK-BREAKDOWN.md §12.2).
/// Vòng lặp không chứa logic nghiệp vụ — uỷ thác qua INotificationService.
/// </summary>
public class PickupReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<PickupReminderService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public PickupReminderService(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        ILogger<PickupReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PickupReminderService bắt đầu khởi chạy (chu kỳ {IntervalSeconds}s).", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval, _clock);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPickupRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp PickupReminderService.");
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

        _logger.LogInformation("PickupReminderService đã dừng.");
    }

    /// <summary>
    /// Thực hiện 1 chu kỳ quét đơn cần nhắc nhở. Mở public để kiểm thử đơn vị độc lập với timer.
    /// </summary>
    public async Task ProcessPickupRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var count = await notificationService.CreatePickupRemindersAsync(cancellationToken);
        if (count > 0)
        {
            _logger.LogInformation("PickupReminderService: Đã tạo {Count} thông báo nhắc nhận hàng.", count);
        }
    }
}
