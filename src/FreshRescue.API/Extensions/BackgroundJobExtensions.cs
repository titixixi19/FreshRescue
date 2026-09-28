namespace FreshRescue.API.Extensions;

/// <summary>
/// Chủ sở hữu: Hiếu (T4-HIEU-01…03). Đăng ký BackgroundService trong <c>BackgroundServices/</c>.
/// Vòng lặp không chứa logic nghiệp vụ — chỉ gọi method của service chủ lát (07-WORK-BREAKDOWN.md §12.2).
/// </summary>
public static class BackgroundJobExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        // TODO(Hieu, 2026-10-06): AddHostedService<SaleBatchSchedulerService>() v.v.
        return services;
    }
}
