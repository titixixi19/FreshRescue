using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
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

    private Guid RequireCustomerId()
        => _currentUser.UserId ?? throw new InvalidOperationException("Endpoint đơn hàng phải yêu cầu đăng nhập.");

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
