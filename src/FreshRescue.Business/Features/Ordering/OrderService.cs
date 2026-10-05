using AutoMapper;
using FreshRescue.Business.Common;
using FreshRescue.Business.Features.Ordering.Dtos;
using FreshRescue.DataAccess.UnitOfWork;
using FreshRescue.Entities.Enums;
using FreshRescue.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private readonly TimeProvider _clock;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUnitOfWork uow,
        IMapper mapper,
        ICurrentUser currentUser,
        TimeProvider clock,
        ILogger<OrderService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.UserId
            ?? throw new InvalidOperationException("POST /orders phải yêu cầu đăng nhập.");

        // NFR-01: tranh chấp tồn kho → nạp lại và thử lại, tối đa 3 lần (02-DOMAIN-MODEL.md §6.1).
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

        // 02 §6.2: luôn xử lý offer theo Id tăng dần. Hai đơn khoá cùng các dòng theo thứ tự khác
        // nhau sẽ deadlock. Sắp ở bộ nhớ để dùng đúng phép so sánh Guid mà EF dùng khi xếp lệnh UPDATE.
        var offers = (await _uow.Offers.Query()
                .Where(o => offerIds.Contains(o.Id))
                .ToListAsync(cancellationToken))
            .OrderBy(o => o.Id)
            .ToList();

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

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
