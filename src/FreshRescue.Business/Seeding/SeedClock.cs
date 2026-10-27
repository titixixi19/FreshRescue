namespace FreshRescue.Business.Seeding;

/// <summary>
/// Đồng hồ cho seeder: đứng yên ở thời điểm được đặt, mặc định chạy theo đồng hồ hệ thống.
/// Seeder tua đồng hồ để dựng lại lịch sử bằng đúng các service thật (đặt đơn chỉ hợp lệ trong khung bán).
/// </summary>
public sealed class SeedClock : TimeProvider
{
    private DateTime? _fixed;

    public void Set(DateTime utc) => _fixed = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

    public void Release() => _fixed = null;

    public override DateTimeOffset GetUtcNow()
        => _fixed is { } t ? new DateTimeOffset(t, TimeSpan.Zero) : base.GetUtcNow();
}
