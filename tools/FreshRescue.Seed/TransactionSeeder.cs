using FreshRescue.Business.Features.Auth.Dtos;
using FreshRescue.Business.Features.Auth.Services;
using FreshRescue.Business.Features.Engagement;
using FreshRescue.Business.Features.Engagement.Dtos;
using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Business.Features.Sales;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.DataAccess;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreshRescue.Seed;

/// <summary>
/// Seeder pha 2 (T5-TV6-02): dữ liệu giao dịch. Mọi bản ghi đi qua <see cref="ISaleBatchService"/>
/// và <see cref="IOrderService"/> thật nên seeder đồng thời là bài kiểm tra tích hợp: quy tắc nghiệp vụ
/// nào bị phá thì seeder ném lỗi chứ không lặng lẽ ghi dữ liệu sai. Cần pha 1 đã chạy xong.
/// </summary>
/// <remarks>
/// Đặt đơn chỉ hợp lệ trong khung bán nên seeder tua <see cref="SeedClock"/> về đúng thời điểm của từng bước.
/// Kết quả: 20 đợt đã đóng (đủ Redeemed / NoShow / Cancelled), 10 đợt đang bán, 2 đợt bị huỷ,
/// 5 đợt Scheduled và 3 bản nháp. Kèm theo đánh giá, khiếu nại (đủ các trạng thái), cửa hàng yêu thích,
/// thông báo đợt bán mới và phiên đăng nhập — để mọi bảng có ít nhất 20 bản ghi (NFR-09).
/// </remarks>
public sealed class TransactionSeeder
{
    // Đợt của seeder nằm ở chi nhánh thuộc thương hiệu demo (chủ là brandmanagerNN@freshrescue.test).
    private const string DemoOwnerPrefix = "brandmanager";
    private const string DemoOwnerDomain = "@freshrescue.test";

    private const int StoreCount = 20;
    private const int CustomersPerBatch = 5;
    private const int OffersPerBatch = 2;
    private const int OfferQuantity = 20;
    private const int OfferMaxPerCustomer = 2;

    private readonly IServiceProvider _root;
    private readonly SeedClock _clock;
    private readonly SeedCurrentUser _user;
    private readonly Func<string, Task> _log;
    private readonly string? _demoPassword;

    private List<StoreInfo> _stores = [];
    private List<Guid> _customers = [];
    private Guid _adminUserId;

    private static readonly string[] _reviewComments =
    [
        "Bánh còn tươi, đáng tiền.",
        "Túi mù nhiều món hơn mong đợi.",
        "Nhân viên thân thiện, lấy hàng nhanh.",
        "Ổn so với giá, sẽ quay lại.",
        "Hơi ít món nhưng vẫn ngon.",
    ];

    private static readonly (DisputeReason Reason, string Description)[] _disputeReasons =
    [
        (DisputeReason.Spoiled, "Một chiếc bánh bị chua, có vẻ đã để quá lâu."),
        (DisputeReason.WrongCategory, "Đặt túi bánh ngọt nhưng nhận toàn đồ mặn."),
        (DisputeReason.Missing, "Thiếu một món so với mô tả trên ứng dụng."),
        (DisputeReason.Other, "Hộp đựng bị móp, bánh dập nát."),
    ];

    public TransactionSeeder(
        IServiceProvider root, SeedClock clock, SeedCurrentUser user, Func<string, Task>? log = null, string? demoPassword = null)
    {
        _root = root;
        _clock = clock;
        _user = user;
        _log = log ?? (_ => Task.CompletedTask);
        _demoPassword = demoPassword;
    }

    /// <summary>Đã có đợt demo thì bỏ qua, chạy lại không nhân đôi dữ liệu.</summary>
    public async Task<bool> AlreadySeededAsync(CancellationToken ct = default)
    {
        using var scope = _root.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FreshRescueDbContext>();
        // Nhận ra lần chạy trước qua chi nhánh demo đã có đợt bán, nên tên đợt không cần gắn nhãn "[Demo]" lộ ra giao diện.
        return await db.SaleBatches.IgnoreQueryFilters().AnyAsync(
            b => b.Store.Brand.Owner.Email!.StartsWith(DemoOwnerPrefix) && b.Store.Brand.Owner.Email.EndsWith(DemoOwnerDomain), ct);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await LoadFoundationAsync(ct);

        var now = TruncateToMinute(_clock.GetUtcNow().UtcDateTime);
        try
        {
            // 0. Khách theo dõi cửa hàng trước khi có đợt bán, để lúc mở đợt sinh thông báo (FR-C12, FR-C13).
            await SeedFavoritesAsync(now.Date.AddDays(-7), ct);
            await _log("Đã dựng cửa hàng yêu thích.");

            // 1. Lịch sử: mỗi chi nhánh một đợt đã đóng, rải trong 5 ngày trước.
            for (var i = 0; i < _stores.Count; i++)
            {
                var saleStart = now.Date.AddDays(-(1 + i % 5)).AddHours(8 + i % 4);
                await SeedClosedBatchAsync(i, saleStart, ct);
            }

            await _log($"Đã dựng {_stores.Count} đợt bán đã đóng.");

            // 2. Hôm nay: chi nhánh 1-10 đang bán, chi nhánh 19-20 bị Brand Manager huỷ cả đợt.
            for (var i = 0; i < 10; i++)
            {
                await SeedActiveBatchAsync(i, now, cancelAfterwards: false, ct);
            }

            for (var i = 18; i < 20; i++)
            {
                await SeedActiveBatchAsync(i, now, cancelAfterwards: true, ct);
            }

            await _log("Đã dựng 10 đợt đang bán và 2 đợt bị huỷ.");

            // 3. Ngày mai: chi nhánh 11-15 đã lên lịch, 16-18 còn là bản nháp.
            for (var i = 10; i < 15; i++)
            {
                await SeedUpcomingBatchAsync(i, now.AddDays(1), activate: true, ct);
            }

            for (var i = 15; i < 18; i++)
            {
                await SeedUpcomingBatchAsync(i, now.AddDays(1), activate: false, ct);
            }

            await _log("Đã dựng 5 đợt Scheduled và 3 bản nháp.");
        }
        finally
        {
            _clock.Release();
        }

        // 4. Phiên đăng nhập thật (bảng RefreshTokens) — sau khi trả đồng hồ về giờ thật để token còn hạn.
        await SeedSessionsAsync(ct);
    }

    private async Task SeedClosedBatchAsync(int storeIndex, DateTime saleStart, CancellationToken ct)
    {
        var store = _stores[storeIndex];
        var times = Times(saleStart);

        var batch = await CreateScheduledAsync(store, times, saleStart.AddHours(-2), "Xả hàng cuối ngày", ct);

        _clock.Set(times.SaleStart.AddMinutes(1));
        await ActivateAndNotifyAsync(store, batch.Id, ct);

        var offerId = batch.Offers[0].Id;
        var customers = CustomersOf(storeIndex);

        // Năm khách đặt lúc mở bán. 0: sẽ nhận hàng · 1: không đến (NoShow) · 2: tự huỷ
        // · 3: không trả tiền (hết hạn) · 4: bị cửa hàng huỷ (chi nhánh chẵn) hoặc nhận hàng.
        _clock.Set(times.SaleStart.AddMinutes(5));
        var orders = new List<OrderDto>();
        foreach (var customer in customers)
        {
            orders.Add(await PlaceOrderAsync(customer, batch.Id, offerId, ct));
        }

        // Chi nhánh thứ ba có một lần thanh toán lỗi trước khi thành công (OQ-17).
        if (storeIndex % 3 == 0)
        {
            _clock.Set(times.SaleStart.AddMinutes(7));
            await PayAsync(customers[0], orders[0].Id, PaymentOutcome.Failure, ct);
        }

        _clock.Set(times.SaleStart.AddMinutes(8));
        var tickets = new Dictionary<int, string?>();
        foreach (var n in new[] { 0, 1, 2, 4 })
        {
            tickets[n] = (await PayAsync(customers[n], orders[n].Id, PaymentOutcome.Success, ct)).QrTicket?.Token;
        }

        _clock.Set(times.SaleStart.AddMinutes(12));
        await InScopeAsync(AsCustomer(customers[2]), s => s.GetRequiredService<IOrderService>().CancelByCustomerAsync(orders[2].Id, ct));

        // Quá hạn thanh toán 10 phút: job huỷ đơn của khách 3 và trả lại suất (FR-X03).
        _clock.Set(times.SaleStart.AddMinutes(25));
        await InScopeAsync(AsSystem(), s => s.GetRequiredService<IOrderService>().ExpireOverdueOrdersAsync(ct));

        var storeCancels = storeIndex % 2 == 0;
        if (storeCancels)
        {
            _clock.Set(times.SaleStart.AddMinutes(30));
            await InScopeAsync(
                AsStaff(store),
                s => s.GetRequiredService<IOrderService>()
                    .CancelByStoreAsync(orders[4].Id, new CancelOrderByStoreRequest("Hết hàng thực tế, xin lỗi khách"), ct));
        }

        // Nhận hàng bằng mã đơn: QrService kiểm hạn vé theo giờ thật, nên vé của đợt quá khứ
        // luôn bị coi là hết hạn dù đồng hồ seeder đã lùi. Quét QR thật chỉ dùng ở đợt đang mở.
        _clock.Set(times.PickupStart.AddMinutes(10));
        await RedeemAsync(store, orders[0], null, ct);
        if (!storeCancels)
        {
            await RedeemAsync(store, orders[4], null, ct);
        }

        // Đánh giá đơn đã nhận (FR-C11, BR-12) và một khiếu nại trong 2 giờ (FR-C15, BR-17).
        _clock.Set(times.PickupStart.AddMinutes(25));
        await ReviewAsync(customers[0], orders[0].Id, (byte)(3 + storeIndex % 3), storeIndex, ct);
        if (!storeCancels)
        {
            await ReviewAsync(customers[4], orders[4].Id, 5, storeIndex + 1, ct);
        }

        _clock.Set(times.PickupStart.AddMinutes(40));
        await SeedDisputeAsync(storeIndex, store, customers[0], orders[0].Id, times, ct);

        // Quá giờ nhận: job đánh dấu NoShow cho khách 1 rồi đóng đợt (FR-X04 rồi FR-X02).
        _clock.Set(times.PickupEnd.AddMinutes(1));
        await InScopeAsync(AsSystem(), s => s.GetRequiredService<ISaleBatchService>().CloseDueBatchesAsync(ct));
    }

    private async Task SeedActiveBatchAsync(int storeIndex, DateTime now, bool cancelAfterwards, CancellationToken ct)
    {
        var store = _stores[storeIndex];
        var times = new BatchTimes(now.AddMinutes(-60), now.AddMinutes(180), now.AddMinutes(-30), now.AddMinutes(240));

        var batch = await CreateScheduledAsync(store, times, now.AddMinutes(-120), "Đợt chiều hôm nay", ct);

        _clock.Set(times.SaleStart.AddMinutes(1));
        await ActivateAndNotifyAsync(store, batch.Id, ct);

        var offerId = batch.Offers[0].Id;
        var customers = CustomersOf(storeIndex);

        _clock.Set(now.AddMinutes(-50));
        var orders = new List<OrderDto>();
        foreach (var customer in customers.Take(4))
        {
            orders.Add(await PlaceOrderAsync(customer, batch.Id, offerId, ct));
        }

        _clock.Set(now.AddMinutes(-45));
        var tickets = new Dictionary<int, string?>();
        foreach (var n in new[] { 0, 1, 3 })
        {
            tickets[n] = (await PayAsync(customers[n], orders[n].Id, PaymentOutcome.Success, ct)).QrTicket?.Token;
        }

        // Khách 3 đổi ý trong cửa sổ huỷ (BR-13).
        _clock.Set(now.AddMinutes(-40));
        await InScopeAsync(AsCustomer(customers[3]), s => s.GetRequiredService<IOrderService>().CancelByCustomerAsync(orders[3].Id, ct));

        // Đợt bị huỷ cả đợt sau đó không được có đơn đã trao (BR-23).
        if (!cancelAfterwards)
        {
            _clock.Set(now.AddMinutes(-20));
            await RedeemAsync(store, orders[0], tickets[0], ct);
        }

        // Khách 4 vừa đặt, chưa trả tiền: đơn ở trạng thái chờ thanh toán lúc seed xong.
        _clock.Set(now.AddMinutes(-5));
        await PlaceOrderAsync(customers[4], batch.Id, offerId, ct);

        _clock.Set(now);
        if (cancelAfterwards)
        {
            await InScopeAsync(
                AsManager(store),
                s => s.GetRequiredService<ISaleBatchService>()
                    .CancelAsync(batch.Id, new CancelSaleBatchRequest("Cửa hàng đóng cửa đột xuất"), ct));
        }
    }

    private async Task SeedFavoritesAsync(DateTime at, CancellationToken ct)
    {
        _clock.Set(at);
        for (var i = 0; i < _customers.Count; i++)
        {
            foreach (var storeIndex in new[] { i % _stores.Count, (i + 7) % _stores.Count })
            {
                var storeId = _stores[storeIndex].Id;
                await InScopeAsync(AsCustomer(_customers[i]), s => s.GetRequiredService<IFavoriteService>().AddFavoriteAsync(storeId, ct));
            }
        }
    }

    // Giống SaleBatchSchedulerService của API: kích hoạt đợt tới giờ rồi báo cho người theo dõi chi nhánh.
    private async Task ActivateAndNotifyAsync(StoreInfo store, Guid batchId, CancellationToken ct)
    {
        await InScopeAsync(AsSystem(), s => s.GetRequiredService<ISaleBatchService>().ActivateDueBatchesAsync(ct));
        await InScopeAsync(
            AsSystem(),
            s => s.GetRequiredService<INotificationService>().NotifyNewSaleBatchAsync(batchId, store.Id, store.Name, ct));
    }

    private Task<ReviewDto> ReviewAsync(Guid customerId, Guid orderId, byte rating, int seed, CancellationToken ct)
        => InScopeAsync(
            AsCustomer(customerId),
            s => s.GetRequiredService<IReviewService>().CreateAsync(
                orderId, new CreateReviewRequest(rating, _reviewComments[seed % _reviewComments.Length], null), ct));

    // Rải đủ trạng thái để màn hình duyệt khiếu nại có dữ liệu: duyệt (hoàn tiền), từ chối, chờ kèm giải trình, chờ.
    private async Task SeedDisputeAsync(
        int storeIndex, StoreInfo store, Guid customerId, Guid orderId, BatchTimes times, CancellationToken ct)
    {
        var (reason, description) = _disputeReasons[storeIndex % _disputeReasons.Length];
        var dispute = await InScopeAsync(
            AsCustomer(customerId),
            s => s.GetRequiredService<IDisputeService>().CreateAsync(
                orderId, new CreateDisputeRequest(reason, description, SeedImages.DisputePhotoUrl), ct));

        var outcome = storeIndex % 4;
        if (outcome == 3)
        {
            return;
        }

        _clock.Set(times.PickupStart.AddMinutes(55));
        await InScopeAsync(
            AsStaff(store),
            s => s.GetRequiredService<IDisputeService>().SetMerchantNoteAsync(
                dispute.Id, new MerchantNoteRequest("Cửa hàng đã kiểm tra lại lô hàng và xin lỗi khách."), ct));

        _clock.Set(times.PickupStart.AddMinutes(90));
        if (outcome == 0)
        {
            await InScopeAsync(AsAdmin(), s => s.GetRequiredService<IDisputeService>().ApproveAsync(dispute.Id, ct));
        }
        else if (outcome == 1)
        {
            await InScopeAsync(
                AsAdmin(),
                s => s.GetRequiredService<IDisputeService>().RejectAsync(
                    dispute.Id, new RejectDisputeRequest("Ảnh chưa cho thấy lỗi của cửa hàng."), ct));
        }
    }

    // Mỗi lần đăng nhập ghi một refresh token — đúng luồng của POST /auth/login.
    private async Task SeedSessionsAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_demoPassword))
        {
            return;
        }

        List<string> emails;
        using (var scope = _root.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FreshRescueDbContext>();
            var roles = await db.Roles
                .Where(r => r.Name == AppRoles.Customer || r.Name == AppRoles.StoreStaff)
                .Select(r => r.Id)
                .ToListAsync(ct);
            emails = await db.Users
                .Where(u => u.Email != null && u.Email.EndsWith("@freshrescue.test")
                    && db.UserRoles.Any(ur => ur.UserId == u.Id && roles.Contains(ur.RoleId)))
                .OrderBy(u => u.Email)
                .Select(u => u.Email!)
                .ToListAsync(ct);
        }

        foreach (var email in emails)
        {
            await InScopeAsync(
                AsSystem(),
                s => s.GetRequiredService<IAuthService>().LoginAsync(new LoginRequest { Email = email, Password = _demoPassword }, ct));
        }

        await _log($"Đã tạo {emails.Count} phiên đăng nhập (refresh token).");
    }

    private async Task SeedUpcomingBatchAsync(int storeIndex, DateTime now, bool activate, CancellationToken ct)
    {
        var store = _stores[storeIndex];
        var times = Times(now.Date.AddHours(17));
        _clock.Release();

        var request = BuildBatchRequest(store, times, "Đợt bán sắp tới");
        var batch = await InScopeAsync(AsStaff(store), s => s.GetRequiredService<ISaleBatchService>().CreateAsync(request, ct));
        if (activate)
        {
            await InScopeAsync(AsStaff(store), s => s.GetRequiredService<ISaleBatchService>().ActivateAsync(batch.Id, ct));
        }
    }

    // Tạo bản nháp rồi kích hoạt sang Scheduled, đúng như nhân viên làm trên giao diện.
    private async Task<SaleBatchDto> CreateScheduledAsync(
        StoreInfo store, BatchTimes times, DateTime createdAt, string title, CancellationToken ct)
    {
        _clock.Set(createdAt);
        var request = BuildBatchRequest(store, times, title);
        var batch = await InScopeAsync(AsStaff(store), s => s.GetRequiredService<ISaleBatchService>().CreateAsync(request, ct));
        await InScopeAsync(AsStaff(store), s => s.GetRequiredService<ISaleBatchService>().ActivateAsync(batch.Id, ct));
        return batch;
    }

    private static CreateSaleBatchRequest BuildBatchRequest(StoreInfo store, BatchTimes times, string title)
        => new(
            title,
            times.SaleStart,
            times.SaleEnd,
            times.PickupStart,
            times.PickupEnd,
            store.Id,
            store.Templates
                .Select(t => new CreateSaleBatchOfferRequest(t.TemplateId, t.ActualPrice, OfferQuantity, OfferMaxPerCustomer))
                .ToList());

    private Task<OrderDto> PlaceOrderAsync(Guid customerId, Guid batchId, Guid offerId, CancellationToken ct)
        => InScopeAsync(
            AsCustomer(customerId),
            s => s.GetRequiredService<IOrderService>()
                .CreateAsync(new CreateOrderRequest(batchId, [new CreateOrderItemRequest(offerId, 1)]), ct));

    private Task<OrderPaymentDto> PayAsync(Guid customerId, Guid orderId, PaymentOutcome outcome, CancellationToken ct)
        => InScopeAsync(
            AsCustomer(customerId),
            s => s.GetRequiredService<IOrderService>().PayAsync(orderId, new PayOrderRequest(outcome), ct));

    private Task<RedemptionDto> RedeemAsync(StoreInfo store, OrderDto order, string? token, CancellationToken ct)
        => InScopeAsync(
            AsStaff(store),
            s => s.GetRequiredService<IOrderService>()
                .RedeemAsync(token is null ? new RedeemOrderRequest(null, order.OrderCode) : new RedeemOrderRequest(token, null), ct));

    private Action AsStaff(StoreInfo store) => () => _user.Act(store.StaffUserId, AppRoles.StoreStaff, store.BrandId, store.Id);

    private Action AsManager(StoreInfo store) => () => _user.Act(store.ManagerUserId, AppRoles.BrandManager, store.BrandId);

    private Action AsCustomer(Guid customerId) => () => _user.Act(customerId, AppRoles.Customer);

    private Action AsAdmin() => () => _user.Act(_adminUserId, AppRoles.SystemAdmin);

    // Job nền không có người dùng đăng nhập.
    private static Action AsSystem() => () => { };

    // Mỗi bước một scope mới, như mỗi request một DbContext.
    private async Task<T> InScopeAsync<T>(Action actAs, Func<IServiceProvider, Task<T>> action)
    {
        actAs();
        using var scope = _root.CreateScope();
        return await action(scope.ServiceProvider);
    }

    private async Task LoadFoundationAsync(CancellationToken ct)
    {
        using var scope = _root.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FreshRescueDbContext>();

        // Chi nhánh của thương hiệu demo (chủ là brandmanagerNN@freshrescue.test); sắp theo email chủ
        // để chi nhánh thứ NN luôn là nơi storestaffNN làm việc, kể cả khi đổi tên hiển thị.
        var stores = await db.Stores
            .Where(s => s.Brand.Owner.Email!.StartsWith(DemoOwnerPrefix) && s.Brand.Owner.Email.EndsWith(DemoOwnerDomain))
            .OrderBy(s => s.Brand.Owner.Email)
            .Select(s => new { s.Id, s.Name, s.BrandId, OwnerUserId = s.Brand.OwnerUserId })
            .ToListAsync(ct);

        var staff = await db.StoreStaff.ToDictionaryAsync(x => x.StoreId, x => x.UserId, ct);
        var templates = await db.OfferTemplates
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.BrandId, t.SuggestedPrice })
            .ToListAsync(ct);

        _stores = stores
            .Where(s => staff.ContainsKey(s.Id))
            .Select(s => new StoreInfo(
                s.Id,
                s.Name,
                s.BrandId,
                s.OwnerUserId,
                staff[s.Id],
                templates.Where(t => t.BrandId == s.BrandId)
                    .Take(OffersPerBatch)
                    .Select(t => new TemplateLine(t.Id, t.SuggestedPrice))
                    .ToList()))
            .Where(s => s.Templates.Count > 0)
            .Take(StoreCount)
            .ToList();

        var adminRole = await db.Roles.SingleAsync(r => r.Name == AppRoles.SystemAdmin, ct);
        _adminUserId = await db.UserRoles.Where(ur => ur.RoleId == adminRole.Id).Select(ur => ur.UserId).FirstAsync(ct);

        var customerRole = await db.Roles.SingleAsync(r => r.Name == AppRoles.Customer, ct);
        _customers = await db.UserRoles
            .Where(ur => ur.RoleId == customerRole.Id)
            .Select(ur => ur.UserId)
            .OrderBy(id => id)
            .ToListAsync(ct);

        if (_stores.Count < StoreCount || _customers.Count < CustomersPerBatch)
        {
            throw new InvalidOperationException(
                $"Chưa đủ dữ liệu pha 1 (cần {StoreCount} chi nhánh có nhân viên và mẫu mặt hàng, "
                + $"{CustomersPerBatch} khách; hiện có {_stores.Count} chi nhánh, {_customers.Count} khách). Chạy seeder pha 1 trước.");
        }
    }

    // Mỗi chi nhánh một nhóm khách xoay vòng để đơn không dồn vào vài người.
    private List<Guid> CustomersOf(int storeIndex)
        => Enumerable.Range(0, CustomersPerBatch)
            .Select(n => _customers[(storeIndex + n) % _customers.Count])
            .ToList();

    private static BatchTimes Times(DateTime saleStart)
        => new(saleStart, saleStart.AddHours(3), saleStart.AddHours(1), saleStart.AddHours(4));

    private static DateTime TruncateToMinute(DateTime value)
        => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc);

    private sealed record BatchTimes(DateTime SaleStart, DateTime SaleEnd, DateTime PickupStart, DateTime PickupEnd);

    private sealed record TemplateLine(Guid TemplateId, decimal ActualPrice);

    private sealed record StoreInfo(Guid Id, string Name, Guid BrandId, Guid ManagerUserId, Guid StaffUserId, List<TemplateLine> Templates);
}
