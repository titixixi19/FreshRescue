using AutoMapper;
using FreshRescue.Business.Features.Catalog;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FreshRescue.Tests.Catalog;

/// <summary>
/// Dữ liệu dùng chung cho test endpoint công khai: một thương hiệu đã duyệt, hai danh mục, và các
/// hàm dựng chi nhánh / đợt bán / offer với mốc thời gian tính từ <see cref="Now"/>.
/// </summary>
internal sealed class StorefrontFixture
{
    public static readonly DateTime Now = new(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

    public FreshRescueDbContext Context { get; }

    public Brand Brand { get; } = new() { Name = "Tiệm bánh Mơ", Slug = "tiem-banh-mo", Status = BrandStatus.Approved };

    public Category Bakery { get; } = new() { Name = "Bánh ngọt", Slug = "banh-ngot" };

    public Category Savory { get; } = new() { Name = "Bánh mặn", Slug = "banh-man" };

    public StorefrontFixture()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Context = new FreshRescueDbContext(options, new FixedTimeProvider(Now));

        Context.AddRange(Brand, Bakery, Savory);
        Context.SaveChanges();
    }

    public StorefrontService Service()
    {
        // Mỗi service một vòng đời theo dõi mới, như mỗi request một scope.
        Context.ChangeTracker.Clear();
        return new StorefrontService(new UnitOfWork(Context), Mapper(), new FixedTimeProvider(Now));
    }

    public Store AddStore(
        string name = "Chi nhánh Nguyễn Huệ",
        decimal lat = 10.7741m,
        decimal lng = 106.7035m,
        StoreStatus status = StoreStatus.Active,
        Brand? brand = null)
    {
        var store = new Store
        {
            BrandId = (brand ?? Brand).Id,
            Name = name,
            AddressLine = "12 Nguyễn Huệ",
            District = "Quận 1",
            City = "TP. Hồ Chí Minh",
            Latitude = lat,
            Longitude = lng,
            Status = status,
        };
        Context.Add(store);
        Context.SaveChanges();
        return store;
    }

    /// <summary>Đợt bán; mặc định đang Active và đang trong khung giờ bán (Now ± 1h).</summary>
    public SaleBatch AddBatch(
        Store store,
        SaleBatchStatus status = SaleBatchStatus.Active,
        int startHoursFromNow = -1,
        int endHoursFromNow = 1)
    {
        var batch = new SaleBatch
        {
            StoreId = store.Id,
            Name = "Xả bánh tối",
            Status = status,
            SaleStartTime = Now.AddHours(startHoursFromNow),
            SaleEndTime = Now.AddHours(endHoursFromNow),
            PickupStartTime = Now.AddHours(startHoursFromNow + 1),
            PickupEndTime = Now.AddHours(endHoursFromNow + 1),
            CreatedByUserId = Guid.NewGuid(),
        };
        Context.Add(batch);
        Context.SaveChanges();
        return batch;
    }

    public Offer AddOffer(
        SaleBatch batch,
        string name = "Túi mù bánh ngọt",
        decimal actualPrice = 35_000,
        int remaining = 5,
        Category? category = null,
        OfferType type = OfferType.SurpriseBag,
        bool deleted = false)
    {
        var offer = new Offer
        {
            SaleBatchId = batch.Id,
            OfferTemplateId = Guid.NewGuid(),
            Name = name,
            CategoryId = (category ?? Bakery).Id,
            OfferType = type,
            OriginalPrice = 100_000,
            ActualPrice = actualPrice,
            TotalQuantity = 10,
            RemainingQuantity = remaining,
            MaxPerCustomer = 3,
            IsDeleted = deleted,
        };
        Context.Add(offer);
        Context.SaveChanges();
        return offer;
    }

    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<StorefrontProfile>()).CreateMapper();

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
