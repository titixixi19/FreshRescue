using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Constants;
using FreshRescue.Entities.Engagement;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreshRescue.Business.Features.Ordering;

public class OrderService : IOrderService
{
    // BR-20: tỉ lệ phí sàn, đóng băng vào từng đơn lúc tạo — đổi tỉ lệ không làm đổi đơn cũ.
    private const decimal PlatformFeeRate = 0.10m;

    private const int MaxAttempts = 3;

    private static readonly TimeSpan _paymentHold = TimeSpan.FromMinutes(10);

    // BR-08: được trao hàng sớm 5 phút trước PickupStartTime.
    private static readonly TimeSpan _earlyRedeem = TimeSpan.FromMinutes(5);

    // BR-13: khách huỷ đơn đã trả tiền trong 15 phút sau khi thanh toán.
    private static readonly TimeSpan _customerCancelWindow = TimeSpan.FromMinutes(15);

    // FR-X03: mỗi lượt job xử lý tối đa chừng này đơn, phần còn lại để lượt sau (mỗi phút một lượt).
    private const int ExpireBatchSize = 100;

    // BR-06: đếm cả Redeemed — khung bán và khung nhận chồng nhau, khách có thể nhận xong rồi đặt tiếp.
    private static readonly OrderStatus[] _purchaseLimitStatuses =
        [OrderStatus.PendingPayment, OrderStatus.Paid, OrderStatus.Redeemed];

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly IQrTicketClient _qrTicketClient;
    private readonly TimeProvider _clock;
    private readonly ILogger<OrderService> _logger;
    private readonly OrderingOptions _options;

    public OrderService(
        IUnitOfWork uow,
        IMapper mapper,
        ICurrentUser currentUser,
        IQrTicketClient qrTicketClient,
        TimeProvider clock,
        ILogger<OrderService> logger,
        IOptions<OrderingOptions> options)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _qrTicketClient = qrTicketClient;
        _clock = clock;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.UserId
            ?? throw new InvalidOperationException("POST /orders phải yêu cầu đăng nhập.");

        // NFR-01: tranh chấp tồn kho → nạp lại và thử lại, tối đa 3 lần (02-DOMAIN-MODEL.md §6.1).
        // Chỉ xảy ra ở chế độ Optimistic; chế độ Pessimistic xếp hàng chờ khoá nên không xung đột.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await PlaceOrderAsync(customerId, request, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                _logger.LogInformation(
                    "Tranh chấp tồn kho khi đặt hàng, thử lại. SaleBatchId={SaleBatchId}, Lần={Attempt}",
                    request.SaleBatchId, attempt);
                _uow.ClearChangeTracker();
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(20, 100) * attempt), _clock, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning(
                    "Đặt hàng thất bại sau {MaxAttempts} lần tranh chấp tồn kho. SaleBatchId={SaleBatchId}",
                    MaxAttempts, request.SaleBatchId);
                _uow.ClearChangeTracker();
                throw AppException.Conflict(
                    ErrorCodes.ConcurrencyConflict,
                    "Mặt hàng vừa được người khác mua hết, vui lòng thử lại.");
            }
        }
    }

    private async Task<OrderDto> PlaceOrderAsync(Guid customerId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

        var batch = await _uow.SaleBatches.Query()
            .Include(b => b.Store)
            .SingleOrDefaultAsync(b => b.Id == request.SaleBatchId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đợt bán.");

        // BR-02
        if (batch.Status != SaleBatchStatus.Active)
        {
            throw AppException.Conflict(ErrorCodes.BatchNotActive, "Đợt bán chưa mở hoặc đã đóng.");
        }

        if (now < batch.SaleStartTime || now > batch.SaleEndTime)
        {
            throw AppException.Conflict(ErrorCodes.SaleWindowClosed, "Đợt bán đã kết thúc.");
        }

        var offerIds = request.Items.Select(i => i.OfferId).ToList();

        var loaded = _options.LockingStrategy == OfferLockingStrategy.Pessimistic
            ? await _uow.Offers.GetForUpdateAsync(offerIds, cancellationToken)
            : await _uow.Offers.Query().Where(o => offerIds.Contains(o.Id)).ToListAsync(cancellationToken);

        // 02 §6.2: luôn xử lý offer theo Id tăng dần. Hai đơn khoá cùng các dòng theo thứ tự khác
        // nhau sẽ deadlock. Sắp ở bộ nhớ để dùng đúng phép so sánh Guid mà EF dùng khi xếp lệnh UPDATE.
        var offers = loaded.OrderBy(o => o.Id).ToList();

        if (offers.Count != offerIds.Count)
        {
            throw AppException.NotFound(ErrorCodes.NotFound, "Có mặt hàng không tồn tại hoặc đã ngừng bán.");
        }

        // BR-03: một đơn — một đợt bán — một mã QR — một quầy.
        if (offers.Any(o => o.SaleBatchId != batch.Id))
        {
            throw AppException.BadRequest(ErrorCodes.OfferCrossBatch, "Mọi món trong một đơn phải thuộc cùng một đợt bán.");
        }

        // BR-06: cộng dồn mọi đơn của khách, không tính theo từng đơn (02 §5.2).
        var alreadyBought = await _uow.OrderItems.Query()
            .Where(i => offerIds.Contains(i.OfferId)
                && i.Order.CustomerUserId == customerId
                && _purchaseLimitStatuses.Contains(i.Order.Status))
            .GroupBy(i => i.OfferId)
            .Select(g => new { OfferId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.OfferId, x => x.Quantity, cancellationToken);

        var requested = request.Items.ToDictionary(i => i.OfferId, i => i.Quantity);

        var order = new Order
        {
            OrderCode = OrderCodeGenerator.Generate(),
            CustomerUserId = customerId,
            StoreId = batch.StoreId,
            SaleBatchId = batch.Id,
            Status = OrderStatus.PendingPayment,
            // BR-05: theo PickupEndTime, không theo SaleEndTime — khách đặt sát giờ đóng vẫn kịp trả tiền.
            PaymentDeadline = Min(now + _paymentHold, batch.PickupEndTime),
            Store = batch.Store,
            SaleBatch = batch,
        };

        foreach (var offer in offers)
        {
            var quantity = requested[offer.Id];

            // BR-01
            if (offer.RemainingQuantity < quantity)
            {
                throw AppException.Conflict(
                    ErrorCodes.OfferSoldOut,
                    $"'{offer.Name}' chỉ còn {offer.RemainingQuantity} suất, bạn đặt {quantity}.");
            }

            var bought = alreadyBought.GetValueOrDefault(offer.Id);
            if (bought + quantity > offer.MaxPerCustomer)
            {
                throw AppException.Conflict(
                    ErrorCodes.PurchaseLimitExceeded,
                    $"Mỗi người chỉ mua tối đa {offer.MaxPerCustomer} suất '{offer.Name}' trong một đợt, bạn đã có {bought}.");
            }

            // BR-04: trừ tồn kho ngay lúc tạo đơn. RowVersion biến lệnh UPDATE thành
            // "WHERE Id = @id AND RowVersion = @v" — ai chậm chân sẽ nhận DbUpdateConcurrencyException.
            offer.RemainingQuantity -= quantity;

            order.Items.Add(new OrderItem
            {
                OfferId = offer.Id,
                Quantity = quantity,
                UnitPrice = offer.ActualPrice,
                LineTotal = offer.ActualPrice * quantity,
                OfferNameSnapshot = offer.Name,
            });
        }

        // BR-20: làm tròn phí trước, phần cửa hàng là phần còn lại — tổng luôn khớp từng đồng.
        // AwayFromZero cho giống ROUND của SQL; mặc định của .NET là làm tròn kiểu ngân hàng.
        order.TotalAmount = order.Items.Sum(i => i.LineTotal);
        order.PlatformFee = Math.Round(order.TotalAmount * PlatformFeeRate, 0, MidpointRounding.AwayFromZero);
        order.StorePayoutAmount = order.TotalAmount - order.PlatformFee;

        await _uow.Orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Đặt hàng thành công. OrderId={OrderId}, OrderCode={OrderCode}, SaleBatchId={SaleBatchId}, TotalAmount={TotalAmount}",
            order.Id, order.OrderCode, batch.Id, order.TotalAmount);

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderPaymentDto> PayAsync(Guid orderId, PayOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();

        var order = await _uow.Orders.Query()
            // BR-22: chi nhánh bị xoá mềm sau khi khách đặt vẫn phải hiện được trên đơn.
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.SaleBatch)
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId && o.CustomerUserId == customerId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw AppException.Conflict(ErrorCodes.OrderNotPayable, "Đơn này không còn chờ thanh toán.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;

        // BR-05. Chỉ từ chối, không tự huỷ ở đây — huỷ và hoàn tồn kho là việc của job FR-X03 (02 §6.4).
        if (now > order.PaymentDeadline)
        {
            throw AppException.Conflict(
                ErrorCodes.PaymentDeadlinePassed,
                "Đã quá hạn thanh toán, suất giữ chỗ sẽ được trả lại. Vui lòng đặt lại.");
        }

        // Thêm qua repository, không qua order.Payments: Id đã sinh sẵn trong BaseEntity nên EF sẽ
        // tưởng bản ghi gắn vào đơn đang theo dõi là bản ghi cũ và sinh UPDATE thay vì INSERT.
        var payment = new Payment
        {
            OrderId = order.Id,
            Method = PaymentMethod.MockGateway,
            Amount = order.TotalAmount,
            TransactionRef = "MOCK-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
        };
        await _uow.Payments.AddAsync(payment, cancellationToken);

        // OQ-17: thất bại thì đơn vẫn giữ chỗ tới hạn thanh toán, khách được bấm lại.
        if (request.Outcome == PaymentOutcome.Failure)
        {
            payment.Status = PaymentStatus.Failed;
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Thanh toán giả lập thất bại. OrderId={OrderId}", order.Id);
            return new OrderPaymentDto { Order = _mapper.Map<OrderDto>(order) };
        }

        // Xin vé trước khi ghi: vé chỉ là một chuỗi đã ký, nên nếu QrService lỗi thì chưa có gì
        // thay đổi trong CSDL và khách bấm lại được. Vé hết hạn cùng khung nhận hàng.
        var expiresAt = order.SaleBatch.PickupEndTime;
        var token = await _qrTicketClient.IssueAsync(order.Id, order.StoreId, expiresAt, cancellationToken);

        payment.Status = PaymentStatus.Succeeded;
        order.Status = OrderStatus.Paid;
        order.PaidAt = now;
        var ticket = new QrTicket
        {
            OrderId = order.Id,
            Token = token,
            IssuedAt = now,
            ExpiresAt = expiresAt,
        };
        await _uow.QrTickets.AddAsync(ticket, cancellationToken);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion: giữa lúc đọc và lúc ghi, job hết hạn đã huỷ đơn hoặc khách bấm thanh toán
            // hai lần. Lệnh UPDATE không khớp dòng nào nên không có gì được ghi.
            _uow.ClearChangeTracker();
            throw AppException.Conflict(ErrorCodes.OrderNotPayable, "Đơn này vừa thay đổi trạng thái, không còn chờ thanh toán.");
        }

        _logger.LogInformation(
            "Thanh toán thành công, đã cấp vé QR. OrderId={OrderId}, OrderCode={OrderCode}",
            order.Id, order.OrderCode);

        return new OrderPaymentDto
        {
            Order = _mapper.Map<OrderDto>(order),
            QrTicket = _mapper.Map<QrTokenDto>(ticket),
        };
    }

    public async Task<QrTicketDto> GetQrTicketAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();

        var ticket = await _uow.QrTickets.Query()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(t => t.Order).ThenInclude(o => o.Store)
            .Include(t => t.Order).ThenInclude(o => o.SaleBatch)
            .SingleOrDefaultAsync(t => t.OrderId == orderId && t.Order.CustomerUserId == customerId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Đơn chưa có vé QR — vé chỉ được cấp sau khi thanh toán thành công.");

        return _mapper.Map<QrTicketDto>(ticket);
    }

    public async Task<RedemptionDto> RedeemAsync(RedeemOrderRequest request, CancellationToken cancellationToken)
    {
        var staffUserId = _currentUser.UserId
            ?? throw new InvalidOperationException("POST /staff/redemptions phải yêu cầu đăng nhập.");

        // Bước 1 (02 §6.5): chữ ký và hạn do QrService kiểm — API không giữ khoá (OQ-22).
        Guid? scannedOrderId = null;
        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            var verification = await _qrTicketClient.VerifyAsync(request.Token.Trim(), cancellationToken);
            if (!verification.Valid)
            {
                throw verification.ErrorCode == ErrorCodes.QrExpired
                    ? AppException.BadRequest(ErrorCodes.QrExpired, "Vé QR đã hết hạn — đã quá giờ nhận hàng của đợt bán.")
                    : AppException.BadRequest(ErrorCodes.QrInvalidSignature, "Mã QR không hợp lệ hoặc đã bị chỉnh sửa.");
            }

            scannedOrderId = verification.OrderId;
        }

        var orders = _uow.Orders.Query()
            .AsNoTracking()
            // BR-22: chi nhánh hoặc offer bị xoá mềm sau khi khách đặt không được làm mất đơn.
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.SaleBatch)
            .Include(o => o.Customer)
            .Include(o => o.Items);

        var orderCode = request.OrderCode?.Trim().ToUpperInvariant();
        var order = await (scannedOrderId is { } id
                ? orders.Where(o => o.Id == id)
                : orders.Where(o => o.OrderCode == orderCode))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        // Bước 2
        EnsureInStoreScope(order);

        // Chỉ để báo lỗi dễ hiểu cho nhân viên — thứ chặn quét hai lần là WHERE ở bước 4.
        EnsureRedeemable(order.Status, order.OrderCode);

        var now = _clock.GetUtcNow().UtcDateTime;

        // Bước 3 — BR-08: mở sớm 5 phút; mốc này khớp với hạn huỷ ở BR-13 để hai cửa sổ không giao nhau.
        if (now < order.SaleBatch.PickupStartTime - _earlyRedeem)
        {
            throw AppException.BadRequest(
                ErrorCodes.RedeemWindowClosed,
                "Chưa tới giờ nhận hàng — chỉ được trao sớm nhất 5 phút trước giờ nhận.");
        }

        if (now > order.SaleBatch.PickupEndTime)
        {
            throw AppException.BadRequest(ErrorCodes.RedeemWindowClosed, "Đã quá giờ nhận hàng của đợt bán.");
        }

        await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

        // Bước 4 — BR-09: idempotent nằm ở mệnh đề WHERE Status = Paid, không ở câu if đọc trước —
        // giữa lúc đọc và lúc ghi luôn có khe hở cho lần quét thứ hai hoặc lệnh huỷ chen vào.
        if (!await _uow.Orders.TryMarkRedeemedAsync(order.Id, now, cancellationToken))
        {
            var current = await _uow.Orders.Query()
                .Where(o => o.Id == order.Id)
                .Select(o => o.Status)
                .SingleAsync(cancellationToken);
            EnsureRedeemable(current, order.OrderCode);
            throw new InvalidOperationException($"Đơn {order.OrderCode} vẫn Paid nhưng không cập nhật được.");
        }

        // Bước 5 — NFR-05: vết đối soát nằm ngay trên vé, không cần bảng nhật ký riêng.
        var ticket = await _uow.QrTickets.Query()
            .SingleAsync(t => t.OrderId == order.Id, cancellationToken);
        ticket.Status = QrTicketStatus.Used;
        ticket.UsedAt = now;
        ticket.UsedByStaffUserId = staffUserId;

        await _uow.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Đối soát thành công. OrderId={OrderId}, OrderCode={OrderCode}, StaffUserId={StaffUserId}",
            order.Id, order.OrderCode, staffUserId);

        return _mapper.Map<RedemptionDto>(order) with { RedeemedAt = now };
    }

    public async Task<OrderDto> CancelByCustomerAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var customerId = RequireCustomerId();

        var order = await QueryOrdersForCancel()
            .SingleOrDefaultAsync(o => o.Id == orderId && o.CustomerUserId == customerId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        var now = _clock.GetUtcNow().UtcDateTime;

        EnsureCancellable(order.Status, order.OrderCode, allowPendingPayment: true);

        // BR-13: mốc −5' khớp với BR-08 để cửa sổ huỷ và cửa sổ trao hàng không bao giờ giao nhau —
        // nếu chồng lên, khách có thể nhận bánh rồi bấm huỷ vẫn được hoàn tiền.
        if (order.Status == OrderStatus.Paid
            && now > Min(order.PaidAt!.Value + _customerCancelWindow, order.SaleBatch.PickupStartTime - _earlyRedeem))
        {
            throw AppException.Conflict(
                ErrorCodes.CancelWindowPassed,
                "Đã quá hạn huỷ: đơn đã thanh toán chỉ huỷ được trong 15 phút sau khi trả tiền và trước giờ nhận hàng 5 phút.");
        }

        await CancelOneAsync(order, CancelledBy.Customer, cancelReason: null, now, allowPendingPayment: true, cancellationToken);

        _logger.LogInformation(
            "Khách huỷ đơn. OrderId={OrderId}, OrderCode={OrderCode}, RefundStatus={RefundStatus}",
            order.Id, order.OrderCode, order.RefundStatus);

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CancelByStoreAsync(Guid orderId, CancelOrderByStoreRequest request, CancellationToken cancellationToken)
    {
        var staffUserId = _currentUser.UserId
            ?? throw new InvalidOperationException("POST /staff/orders/{id}/cancel phải yêu cầu đăng nhập.");

        var order = await QueryOrdersForCancel()
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đơn hàng.");

        EnsureInStoreScope(order);

        // BR-16: chỉ đơn Paid chưa trao. Đơn chưa trả tiền để job FR-X03 tự thu hồi.
        EnsureCancellable(order.Status, order.OrderCode, allowPendingPayment: false);

        var now = _clock.GetUtcNow().UtcDateTime;
        await CancelOneAsync(order, CancelledBy.Store, request.Reason.Trim(), now, allowPendingPayment: false, cancellationToken);

        _logger.LogInformation(
            "Cửa hàng huỷ đơn. OrderId={OrderId}, OrderCode={OrderCode}, StaffUserId={StaffUserId}",
            order.Id, order.OrderCode, staffUserId);

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<int> CancelBatchOrdersAsync(Guid saleBatchId, string reason, CancellationToken cancellationToken)
    {
        // BR-23
        if (await _uow.Orders.Query().AnyAsync(o => o.SaleBatchId == saleBatchId && o.Status == OrderStatus.Redeemed, cancellationToken))
        {
            throw AppException.Conflict(
                ErrorCodes.BatchHasRedeemedOrders,
                "Đợt bán đã có đơn được trao hàng, không huỷ cả đợt được nữa.");
        }

        var orders = await QueryOrdersForCancel()
            .Where(o => o.SaleBatchId == saleBatchId
                && (o.Status == OrderStatus.PendingPayment || o.Status == OrderStatus.Paid))
            .ToListAsync(cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;
        var restock = new Dictionary<Guid, int>();
        var cancelled = 0;

        foreach (var order in orders)
        {
            // Đơn vừa đổi trạng thái giữa lúc đọc và lúc ghi (khách vừa huỷ, job vừa thu hồi) thì bỏ qua.
            if (await TryCancelAsync(order, CancelledBy.Store, reason.Trim(), now, restock, cancellationToken))
            {
                cancelled++;
            }
        }

        // Gộp hoàn tồn kho của cả đợt vào một lượt để khoá offer đúng một thứ tự (02 §6.2).
        await _uow.Offers.RestockAsync(restock, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Huỷ cả đợt bán. SaleBatchId={SaleBatchId}, SoDonHuy={CancelledCount}",
            saleBatchId, cancelled);

        return cancelled;
    }

    public async Task<int> ExpireOverdueOrdersAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var overdue = await QueryOrdersForCancel()
            .Where(o => o.Status == OrderStatus.PendingPayment && o.PaymentDeadline < now)
            .OrderBy(o => o.PaymentDeadline)
            .Take(ExpireBatchSize)
            .ToListAsync(cancellationToken);

        var expired = 0;
        foreach (var order in overdue)
        {
            // Mỗi đơn một giao dịch ngắn: không giữ khoá offer của cả lô trong lúc khách khác đang đặt hàng.
            await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

            var restock = new Dictionary<Guid, int>();
            if (!await TryCancelAsync(order, CancelledBy.System, cancelReason: null, now, restock, cancellationToken))
            {
                // Khách vừa thanh toán kịp, hoặc job chạy chồng lượt — đơn đã được xử lý.
                continue;
            }

            await _uow.Offers.RestockAsync(restock, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            expired++;
        }

        if (expired > 0)
        {
            _logger.LogInformation("Đã huỷ {ExpiredCount} đơn quá hạn thanh toán (FR-X03).", expired);
        }

        return expired;
    }

    private IQueryable<Order> QueryOrdersForCancel()
        => _uow.Orders.Query()
            .AsNoTracking()
            // BR-22: chi nhánh bị xoá mềm sau khi khách đặt vẫn phải huỷ và hoàn được.
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.SaleBatch)
            .Include(o => o.Items);

    /// <summary>Huỷ một đơn trong giao dịch riêng; báo lỗi theo trạng thái mới nếu bị chen ngang.</summary>
    private async Task CancelOneAsync(
        Order order,
        CancelledBy cancelledBy,
        string? cancelReason,
        DateTime now,
        bool allowPendingPayment,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

        var restock = new Dictionary<Guid, int>();
        if (!await TryCancelAsync(order, cancelledBy, cancelReason, now, restock, cancellationToken))
        {
            var current = await _uow.Orders.Query()
                .Where(o => o.Id == order.Id)
                .Select(o => o.Status)
                .SingleAsync(cancellationToken);
            EnsureCancellable(current, order.OrderCode, allowPendingPayment);

            // Trạng thái vẫn huỷ được nghĩa là đơn vừa chuyển PendingPayment → Paid: hoàn tiền khác hẳn
            // chỉ trả tồn kho, nên để khách xem lại đơn rồi bấm lại.
            throw AppException.Conflict(ErrorCodes.ConcurrencyConflict, $"Đơn {order.OrderCode} vừa đổi trạng thái, vui lòng tải lại rồi thử lại.");
        }

        await _uow.Offers.RestockAsync(restock, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Bước chung của BR-13, BR-16, BR-23, FR-X03. Không lưu, không hoàn tồn kho ngay — dồn số cần
    /// hoàn vào <paramref name="restock"/> để người gọi hoàn một lượt.
    /// </summary>
    private async Task<bool> TryCancelAsync(
        Order order,
        CancelledBy cancelledBy,
        string? cancelReason,
        DateTime now,
        Dictionary<Guid, int> restock,
        CancellationToken cancellationToken)
    {
        var wasPaid = order.Status == OrderStatus.Paid;

        // 02 §4.3: đã trả tiền thì huỷ là hoàn 100%; chưa trả thì chỉ trả tồn kho.
        var refundStatus = wasPaid ? RefundStatus.Refunded : RefundStatus.None;

        // 02 §6.4: chỉ hoàn tồn kho khi chính lệnh UPDATE này đổi đúng một dòng — chạy hai lần
        // trên cùng đơn thì lần sau không khớp WHERE và không cộng thêm gì.
        if (!await _uow.Orders.TryCancelAsync(order.Id, order.Status, cancelledBy, cancelReason, refundStatus, now, cancellationToken))
        {
            return false;
        }

        foreach (var item in order.Items)
        {
            restock[item.OfferId] = restock.GetValueOrDefault(item.OfferId) + item.Quantity;
        }

        if (wasPaid)
        {
            var ticket = await _uow.QrTickets.Query().SingleOrDefaultAsync(t => t.OrderId == order.Id, cancellationToken);
            if (ticket is not null)
            {
                ticket.Status = QrTicketStatus.Revoked;
            }

            // BR-16: cửa hàng huỷ đơn đã trả tiền thì phải báo khách.
            if (cancelledBy == CancelledBy.Store)
            {
                await _uow.Notifications.AddAsync(
                    new Notification
                    {
                        UserId = order.CustomerUserId,
                        Type = NotificationType.OrderCancelledByStore,
                        Title = $"Đơn {order.OrderCode} đã bị cửa hàng huỷ",
                        Body = $"{order.Store.Name} đã huỷ đơn của bạn. Lý do: {cancelReason}. Bạn được hoàn 100% số tiền đã trả.",
                        RelatedEntityId = order.Id,
                    },
                    cancellationToken);
            }
        }

        // Bản đọc AsNoTracking — cập nhật để trả về đúng trạng thái mới, không ảnh hưởng SaveChanges.
        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = cancelledBy;
        order.CancelReason = cancelReason;
        order.RefundStatus = refundStatus;
        order.CancelledAt = now;
        return true;
    }

    private static void EnsureCancellable(OrderStatus status, string orderCode, bool allowPendingPayment)
    {
        switch (status)
        {
            case OrderStatus.Paid:
                return;
            case OrderStatus.PendingPayment when allowPendingPayment:
                return;
            case OrderStatus.PendingPayment:
                throw AppException.Conflict(
                    ErrorCodes.CancelWindowPassed,
                    $"Đơn {orderCode} chưa thanh toán — hệ thống sẽ tự huỷ khi quá hạn thanh toán.");
            case OrderStatus.Redeemed:
                throw AppException.Conflict(ErrorCodes.OrderAlreadyRedeemed, $"Đơn {orderCode} đã trao hàng, không huỷ được nữa.");
            case OrderStatus.Cancelled:
                throw AppException.Conflict(ErrorCodes.CancelWindowPassed, $"Đơn {orderCode} đã bị huỷ trước đó.");
            default:
                throw AppException.Conflict(ErrorCodes.CancelWindowPassed, $"Đơn {orderCode} đã quá giờ nhận hàng, không huỷ được nữa.");
        }
    }

    /// <summary>BR-10: Staff chỉ chi nhánh mình; Brand Manager mọi chi nhánh của thương hiệu mình.</summary>
    private void EnsureInStoreScope(Order order)
    {
        var inScope = (_currentUser.IsInRole(AppRoles.StoreStaff) && _currentUser.StoreId == order.StoreId)
            || (_currentUser.IsInRole(AppRoles.BrandManager) && _currentUser.BrandId == order.Store.BrandId);
        if (!inScope)
        {
            throw AppException.Forbidden(
                ErrorCodes.ForbiddenScope,
                $"Đơn {order.OrderCode} thuộc chi nhánh '{order.Store.Name}', không phải chi nhánh của bạn.");
        }
    }

    private static void EnsureRedeemable(OrderStatus status, string orderCode)
    {
        switch (status)
        {
            case OrderStatus.Paid:
                return;
            case OrderStatus.Redeemed:
                throw AppException.Conflict(ErrorCodes.OrderAlreadyRedeemed, $"Đơn {orderCode} đã được trao hàng rồi.");
            default:
                // Contract chưa có mã riêng cho trường hợp này — chốt dùng NOT_FOUND (14/10).
                var reason = status switch
                {
                    OrderStatus.PendingPayment => "chưa thanh toán",
                    OrderStatus.Cancelled => "đã bị huỷ",
                    _ => "đã quá hạn nhận hàng",
                };
                throw AppException.NotFound(ErrorCodes.NotFound, $"Đơn {orderCode} {reason} — không có vé hợp lệ để trao hàng.");
        }
    }

    private Guid RequireCustomerId()
        => _currentUser.UserId ?? throw new InvalidOperationException("Endpoint đơn hàng phải yêu cầu đăng nhập.");

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
