using FluentAssertions;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.Tests.DataAccess;

public class UnitOfWorkTests
{
    private static readonly DateTime _now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    private static FreshRescueDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FreshRescueDbContext(options, new FixedTimeProvider(_now));
    }

    // OQ-06: xoá cứng bảng ISoftDelete sẽ làm đơn cũ trỏ vào khoảng không.
    [Fact]
    public async Task Remove_BangSoftDelete_ThanhXoaMemVaBiLocKhoiTruyVanAsync()
    {
        await using var context = CreateContext();
        var uow = new UnitOfWork(context);
        var category = new Category { Name = "Bánh ngọt", Slug = "banh-ngot" };
        await uow.Categories.AddAsync(category, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync(CancellationToken.None);

        var stored = await context.Categories.IgnoreQueryFilters().SingleAsync(c => c.Id == category.Id);
        stored.IsDeleted.Should().BeTrue();
        stored.DeletedAt.Should().Be(_now);
        (await uow.Categories.Query().AnyAsync()).Should().BeFalse();
    }

    // Bảng chỉ có CreatedAt (IHasCreatedAt) cũng phải được gán tự động — 02 §1.1.
    [Fact]
    public async Task SaveChanges_GanCreatedAtChoBangChiCoCreatedAtAsync()
    {
        await using var context = CreateContext();
        var uow = new UnitOfWork(context);
        var notification = new Notification
        {
            UserId = Guid.NewGuid(),
            Title = "Đợt bán mới",
            Body = "Cửa hàng bạn thích vừa mở đợt bán.",
            Type = NotificationType.NewSaleBatch,
        };

        await uow.Notifications.AddAsync(notification, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        notification.CreatedAt.Should().Be(_now);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
