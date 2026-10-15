using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Sales;
using FreshRescue.DataAccess;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Catalog;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FreshRescue.Tests.Sales;

/// <summary>
/// Dữ liệu và dựng service dùng chung cho test đợt bán / offer: một thương hiệu đã duyệt, một
/// chi nhánh, một mẫu túi mù (giá gốc 105.000đ → giá hợp lệ 26.250–36.750đ theo tầng sàn 65–75%).
/// </summary>
internal sealed class SalesFixture
{
    public static readonly DateTime Now = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    public Guid BrandId { get; } = Guid.NewGuid();

    public Guid UserId { get; } = Guid.NewGuid();

    public FreshRescueDbContext Context { get; }

    public Brand Brand { get; }

    public Store Store { get; }

    public OfferTemplate Template { get; }

    public Mock<IOrderService> OrderService { get; } = new();

    public SalesFixture()
    {
        var options = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Context = new FreshRescueDbContext(options, new FixedTimeProvider(Now));

        Brand = new Brand { Id = BrandId, Name = "Tiệm bánh Mơ", Slug = "tiem-banh-mo", Status = BrandStatus.Approved };
        Store = new Store { BrandId = BrandId, Name = "Chi nhánh Nguyễn Huệ", Status = StoreStatus.Active };
        Template = new OfferTemplate
        {
            BrandId = BrandId,
            CategoryId = Guid.NewGuid(),
            Name = "Túi mù bánh ngọt",
            OfferType = OfferType.SurpriseBag,
            OriginalPrice = 105_000,
            SuggestedPrice = 35_000,
        };

        Context.AddRange(Brand, Store, Template);
        Context.SaveChanges();

        // Giả lập OrderService.AdjustOfferStockAsync (một câu UPDATE có điều kiện trong sản phẩm thật).
        OrderService
            .Setup(s => s.AdjustOfferStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid offerId, int newTotal, CancellationToken ct) =>
            {
                var offer = await Context.Offers.SingleAsync(o => o.Id == offerId, ct);
                if (offer.RemainingQuantity + (newTotal - offer.TotalQuantity) < 0)
                {
                    throw AppException.Conflict(ErrorCodes.OfferHasOrders, "Đã có đơn giữ chỗ hoặc bán.");
                }

                offer.RemainingQuantity += newTotal - offer.TotalQuantity;
                offer.TotalQuantity = newTotal;
                await Context.SaveChangesAsync(ct);
            });
    }

    public Mock<ICurrentUser> StaffUser() => User([AppRoles.StoreStaff], storeId: Store.Id);

    public Mock<ICurrentUser> ManagerUser(Guid? brandId = null) => User([AppRoles.BrandManager], brandId: brandId ?? BrandId);

    public SaleBatchService BatchService(Mock<ICurrentUser> user)
        => new(
            Uow(),
            OrderService.Object,
            Mapper(),
            user.Object,
            new FixedTimeProvider(Now),
            NullLogger<SaleBatchService>.Instance);

    public OfferService OfferService(Mock<ICurrentUser> user)
        => new(Uow(), OrderService.Object, Mapper(), user.Object, NullLogger<OfferService>.Instance);

    /// <summary>Đợt bán sẵn trong CSDL, mặc định ở tương lai và không chồng với đợt nào.</summary>
    public async Task<SaleBatch> SeedBatchAsync(
        SaleBatchStatus status = SaleBatchStatus.Draft,
        int startInHours = 2,
        int saleHours = 2,
        bool withOffer = true)
    {
        var start = Now.AddHours(startInHours);
        var batch = new SaleBatch
        {
            StoreId = Store.Id,
            Name = "Xả bánh tối",
            Status = status,
            SaleStartTime = start,
            SaleEndTime = start.AddHours(saleHours),
            PickupStartTime = start.AddHours(1),
            PickupEndTime = start.AddHours(saleHours + 1),
            CreatedByUserId = UserId,
        };
        Context.Add(batch);

        if (withOffer)
        {
            Context.Add(new Offer
            {
                SaleBatchId = batch.Id,
                OfferTemplateId = Template.Id,
                Name = Template.Name,
                CategoryId = Template.CategoryId,
                OfferType = OfferType.SurpriseBag,
                OriginalPrice = 105_000,
                ActualPrice = 35_000,
                TotalQuantity = 10,
                RemainingQuantity = 10,
                MaxPerCustomer = 3,
            });
        }

        await Context.SaveChangesAsync();
        return batch;
    }

    public async Task<Offer> OfferOfAsync(Guid batchId)
        => await Context.Offers.AsNoTracking().SingleAsync(o => o.SaleBatchId == batchId);

    /// <summary>Một đơn của đợt; <paramref name="paid"/> đặt PaidAt như đơn đã thanh toán.</summary>
    public async Task<Order> SeedOrderAsync(Guid batchId, OrderStatus status, bool paid, Guid? offerId = null)
    {
        var order = new Order
        {
            OrderCode = $"FR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CustomerUserId = Guid.NewGuid(),
            StoreId = Store.Id,
            SaleBatchId = batchId,
            Status = status,
            PaidAt = paid ? Now.AddMinutes(-5) : null,
            PaymentDeadline = Now.AddMinutes(10),
            TotalAmount = 35_000,
        };

        if (offerId.HasValue)
        {
            order.Items.Add(new OrderItem
            {
                OfferId = offerId.Value,
                Quantity = 1,
                UnitPrice = 35_000,
                LineTotal = 35_000,
                OfferNameSnapshot = Template.Name,
            });
        }

        Context.Add(order);
        await Context.SaveChangesAsync();
        return order;
    }

    private UnitOfWork Uow()
    {
        // Mỗi service một vòng đời theo dõi mới, như mỗi request một scope.
        Context.ChangeTracker.Clear();
        return new UnitOfWork(Context);
    }

    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<SalesProfile>()).CreateMapper();

    private static Mock<ICurrentUser> User(string[] roles, Guid? storeId = null, Guid? brandId = null)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(Guid.NewGuid());
        user.Setup(u => u.Roles).Returns(roles);
        user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(roles.Contains);
        user.Setup(u => u.StoreId).Returns(storeId);
        user.Setup(u => u.BrandId).Returns(brandId);
        return user;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
