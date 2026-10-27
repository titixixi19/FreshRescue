using FreshRescue.Business.Features.Ordering;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.Business.Features.Sales;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.DataAccess;
using FreshRescue.Entities.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreshRescue.Business.Seeding;

/// <summary>
/// Seeder pha 2 (T5-TV6-02): dữ liệu giao dịch. Mọi bản ghi đi qua <see cref="ISaleBatchService"/>
/// và <see cref="IOrderService"/> thật nên seeder đồng thời là bài kiểm tra tích hợp: quy tắc nghiệp vụ
/// nào bị phá thì seeder ném lỗi chứ không lặng lẽ ghi dữ liệu sai. Cần pha 1 đã chạy xong.
/// </summary>
/// <remarks>
/// Đặt đơn chỉ hợp lệ trong khung bán nên seeder tua <see cref="SeedClock"/> về đúng thời điểm của từng bước.
/// Kết quả: 20 đợt đã đóng (đủ Redeemed / NoShow / Cancelled), 10 đợt đang bán, 2 đợt bị huỷ,
/// 5 đợt Scheduled và 3 bản nháp.
/// </remarks>
public sealed class TransactionSeeder
{
    public const string BatchNamePrefix = "[Demo] ";

    private const int StoreCount = 20;
    private const int CustomersPerBatch = 5;
    private const int OffersPerBatch = 2;
    private const int OfferQuantity = 20;
    private const int OfferMaxPerCustomer = 2;

    private readonly IServiceProvider _root;
    private readonly SeedClock _clock;
    private readonly SeedCurrentUser _user;
    private readonly Func<string, Task> _log;

    private List<StoreInfo> _stores = [];
    private List<Guid> _customers = [];

    public TransactionSeeder(IServiceProvider root, SeedClock clock, SeedCurrentUser user, Func<string, Task>? log = null)
    {
        _root = root;
        _clock = clock;
        _user = user;
        _log = log ?? (_ => Task.CompletedTask);
    }

    /// <summary>Đã có đợt demo thì bỏ qua, chạy lại không nhân đôi dữ liệu.</summary>
    public async Task<bool> AlreadySeededAsync(CancellationToken ct = default)
    {
        using var scope = _root.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FreshRescueDbContext>();
        return await db.SaleBatches.IgnoreQueryFilters().AnyAsync(b => b.Name != null && b.Name.StartsWith(BatchNamePrefix), ct);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await LoadFoundationAsync(ct);

        var now = TruncateToMinute(DateTime.UtcNow);
        try
        {
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
    }

    private async Task SeedClosedBatchAsync(int storeIndex, DateTime saleStart, CancellationToken ct)
    {
        var store = _stores[storeIndex];
        var times = Times(saleStart);

        var batch = await CreateScheduledAsync(store, times, saleStart.AddHours(-2), "Xả hàng cuối ngày", ct);

        _clock.Set(times.SaleStart.AddMinutes(1));
        await InScopeAsync(AsSystem(), s => s.GetRequiredService<ISaleBatchService>().ActivateDueBatchesAsync(ct));

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

        // Nhận hàng: lúc quét mã QR (chi nhánh chẵn), lúc nhập mã đơn thủ công (chi nhánh lẻ).
        _clock.Set(times.PickupStart.AddMinutes(10));
        await RedeemAsync(store, orders[0], storeCancels ? tickets[0] : null, ct);
        if (!storeCancels)
        {
            await RedeemAsync(store, orders[4], null, ct);
        }

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
        await InScopeAsync(AsSystem(), s => s.GetRequiredService<ISaleBatchService>().ActivateDueBatchesAsync(ct));

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
            BatchNamePrefix + title,
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

        var stores = await db.Stores
            .Where(s => s.Name.StartsWith("Chi nhánh Demo"))
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.BrandId, OwnerUserId = s.Brand.OwnerUserId })
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

    private sealed record StoreInfo(Guid Id, Guid BrandId, Guid ManagerUserId, Guid StaffUserId, List<TemplateLine> Templates);
}
