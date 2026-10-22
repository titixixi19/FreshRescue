using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Mapping;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.Engagement;

internal sealed class EngagementFixture
{
    public static readonly DateTime Now = new(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc);

    public Guid CustomerId { get; } = Guid.NewGuid();
    public Guid BrandId { get; } = Guid.NewGuid();
    public FreshRescueDbContext Context { get; }
    public Brand Brand { get; }
    public Store Store { get; }
    public AppUser Customer { get; }

    public EngagementFixture()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new FreshRescueDbContext(options, new FixedTimeProvider(Now));

        Customer = new AppUser
        {
            Id = CustomerId,
            UserName = "customer@freshrescue.vn",
            Email = "customer@freshrescue.vn",
            FullName = "Nguyễn Văn Khách",
            CreatedAt = Now.AddDays(-10),
        };

        Brand = new Brand
        {
            Id = BrandId,
            Name = "Tous Les Jours",
            Slug = "tous-les-jours",
            LogoUrl = "/uploads/brands/tous-les-jours.png",
            Status = FreshRescue.Entities.Enums.BrandStatus.Approved,
            CreatedAt = Now.AddDays(-30),
        };

        Store = new Store
        {
            Id = Guid.NewGuid(),
            BrandId = BrandId,
            Brand = Brand,
            Name = "Tous Les Jours Hai Bà Trưng",
            AddressLine = "180 Hai Bà Trưng",
            Ward = "Phường Đa Kao",
            District = "Quận 1",
            City = "TP. Hồ Chí Minh",
            RatingAverage = 4.8m,
            RatingCount = 120,
            CreatedAt = Now.AddDays(-20),
        };

        Context.AddRange(Customer, Brand, Store);
        Context.SaveChanges();
    }

    public FavoriteService FavoriteService(Guid? userId = null)
    {
        var effectiveUserId = userId ?? CustomerId;
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(effectiveUserId);
        currentUser.Setup(u => u.IsInRole(AppRoles.Customer)).Returns(true);

        return new FavoriteService(
            Uow(),
            currentUser.Object,
            new FixedTimeProvider(Now),
            Mapper(),
            NullLogger<FavoriteService>.Instance);
    }

    public NotificationService NotificationService(Guid? userId = null)
    {
        var effectiveUserId = userId ?? CustomerId;
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(effectiveUserId);
        currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(true);

        return new NotificationService(
            Uow(),
            currentUser.Object,
            new FixedTimeProvider(Now),
            Mapper(),
            NullLogger<NotificationService>.Instance);
    }

    public UnitOfWork Uow()
    {
        Context.ChangeTracker.Clear();
        return new UnitOfWork(Context);
    }

    public static IMapper Mapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EngagementProfile>());
        return config.CreateMapper();
    }

    public sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
