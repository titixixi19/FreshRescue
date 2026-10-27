using System.Globalization;
using System.Text.Json;
using FreshRescue.WebClient.Models.Api;
using Microsoft.Extensions.Primitives;

namespace FreshRescue.WebClient.Services.Mock;

/// <summary>
/// Dữ liệu giả trong bộ nhớ cho <see cref="MockApiClient"/>, sống suốt vòng đời ứng dụng nên
/// đặt hàng xong vẫn thấy trong lịch sử. Luật nghiệp vụ chỉ giả lập đủ để giao diện có
/// đường đi giống API thật — nguồn sự thật vẫn là 03-API-CONTRACT.md và FreshRescue.API.
/// </summary>
/// <remarks>Tài khoản demo — mật khẩu đều là <see cref="DemoPassword"/>: khach@demo.vn, nhanvien@demo.vn, thuonghieu@demo.vn, admin@demo.vn.</remarks>
public class MockApiStore
{
    public const string DemoPassword = "123456";

    private const int PaymentMinutes = 10;
    private const string OrderCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static readonly TimeSpan _accessTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly object _gate = new();
    private readonly TimeProvider _clock;
    private readonly List<Account> _accounts = [];
    private readonly List<CategoryDto> _categories = [];
    private readonly List<Store> _stores = [];
    private readonly List<Offer> _offers = [];
    private readonly List<Order> _orders = [];
    private readonly Dictionary<string, Guid> _refreshTokens = new(StringComparer.Ordinal);

    public MockApiStore(TimeProvider clock)
    {
        _clock = clock;
        Seed();
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    // ---------- Auth (03 §2) ----------

    public AuthResponse Login(LoginRequest request)
    {
        lock (_gate)
        {
            var account = _accounts.FirstOrDefault(a =>
                string.Equals(a.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)
                && a.Password == request.Password);

            return account is null
                ? throw Error(StatusCodes.Status401Unauthorized, ApiErrorMessages.Unauthenticated, "Email hoặc mật khẩu không đúng.")
                : IssueTokens(account);
        }
    }

    public AuthResponse Register(RegisterCustomerRequest request)
    {
        lock (_gate)
        {
            if (_accounts.Any(a => string.Equals(a.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApiException(
                    StatusCodes.Status400BadRequest,
                    ApiErrorMessages.ValidationFailed,
                    "Email đã được sử dụng.",
                    new Dictionary<string, string[]> { ["email"] = ["Email đã được sử dụng."] });
            }

            var account = new Account(Guid.NewGuid(), request.FullName.Trim(), request.Email.Trim(), request.Password, [AppRoles.Customer]);
            _accounts.Add(account);
            return IssueTokens(account);
        }
    }

    public AuthResponse Refresh(RefreshTokenRequest request)
    {
        lock (_gate)
        {
            // Xoay vòng: token cũ dùng một lần là hết (03 §1.5).
            if (!_refreshTokens.Remove(request.RefreshToken, out var userId))
            {
                throw Error(StatusCodes.Status401Unauthorized, ApiErrorMessages.Unauthenticated, "Refresh token không hợp lệ.");
            }

            return IssueTokens(_accounts.First(a => a.Id == userId));
        }
    }

    public object Logout(RefreshTokenRequest request)
    {
        lock (_gate)
        {
            _refreshTokens.Remove(request.RefreshToken);
            return new { message = "Đăng xuất thành công." };
        }
    }

    public UserInfo GetUser(Guid userId)
    {
        lock (_gate)
        {
            return ToUserInfo(_accounts.FirstOrDefault(a => a.Id == userId)
                ?? throw Error(StatusCodes.Status401Unauthorized, ApiErrorMessages.Unauthenticated, null));
        }
    }

    // ---------- Danh mục & offer (03 §3) ----------

    public PagedResponse<CategoryDto> GetCategories()
    {
        lock (_gate)
        {
            return Page(_categories, 1, PagedResponse<CategoryDto>.MaxPageSize);
        }
    }

    public PagedResponse<ActiveOfferDto> GetActiveOffers(IDictionary<string, StringValues> query)
    {
        var storeId = ReadGuid(query, "storeId");
        var categoryId = ReadGuid(query, "categoryId");
        var offerType = Enum.TryParse<OfferType>(query.TryGetValue("offerType", out var type) ? type.ToString() : null, out var parsedType)
            ? parsedType
            : (OfferType?)null;
        var minPrice = ReadDouble(query, "minPrice");
        var maxPrice = ReadDouble(query, "maxPrice");
        var lat = ReadDouble(query, "lat");
        var lng = ReadDouble(query, "lng");
        var radiusKm = ReadDouble(query, "radiusKm") ?? 5;
        var inStockOnly = query.TryGetValue("inStockOnly", out var inStock) && bool.TryParse(inStock, out var flag) && flag;

        lock (_gate)
        {
            var now = Now;
            var rows = _offers
                .Where(o => o.SaleStartTime <= now && now <= o.SaleEndTime)
                .Where(o => storeId is null || o.Store.Id == storeId)
                .Where(o => categoryId is null || o.Category.Id == categoryId)
                .Where(o => offerType is null || o.OfferType == offerType)
                .Where(o => minPrice is null || (double)o.ActualPrice >= minPrice)
                .Where(o => maxPrice is null || (double)o.ActualPrice <= maxPrice)
                .Where(o => !inStockOnly || o.RemainingQuantity > 0)
                .Select(o => (Offer: o, Distance: lat is null || lng is null ? (double?)null : DistanceKm(lat.Value, lng.Value, o.Store)))
                .Where(x => x.Distance is null || x.Distance <= radiusKm)
                .OrderBy(x => x.Offer.RemainingQuantity == 0)
                .ThenBy(x => x.Distance ?? 0)
                .ThenBy(x => x.Offer.PickupStartTime)
                .Select(x => ToDto(x.Offer, x.Distance))
                .ToList();

            return Page(rows, ReadInt(query, "page") ?? 1, ReadInt(query, "pageSize") ?? 20);
        }
    }

    public ActiveOfferDto GetOffer(Guid offerId)
    {
        lock (_gate)
        {
            return ToDto(FindOffer(offerId), null);
        }
    }

    // ---------- Đặt hàng (03 §4) ----------

    public OrderDto CreateOrder(Guid customerId, CreateOrderRequest request)
    {
        if (request.Items is not { Count: >= 1 and <= 20 }
            || request.Items.Any(i => i.Quantity is < 1 or > 10)
            || request.Items.Select(i => i.OfferId).Distinct().Count() != request.Items.Count)
        {
            throw Error(
                StatusCodes.Status400BadRequest,
                ApiErrorMessages.ValidationFailed,
                "Đơn phải có 1–20 món khác nhau, mỗi món 1–10 suất.");
        }

        lock (_gate)
        {
            ExpireOverdueOrders();
            var now = Now;
            var lines = request.Items.Select(i => (Offer: FindOffer(i.OfferId), i.Quantity)).ToList();

            if (lines.Any(l => l.Offer.SaleBatchId != request.SaleBatchId))
            {
                throw Error(StatusCodes.Status400BadRequest, "OFFER_CROSS_BATCH", null);
            }

            var batch = lines[0].Offer;
            if (now < batch.SaleStartTime || now > batch.SaleEndTime)
            {
                throw Error(StatusCodes.Status409Conflict, "SALE_WINDOW_CLOSED", null);
            }

            foreach (var (offer, quantity) in lines)
            {
                if (offer.RemainingQuantity < quantity)
                {
                    throw Error(
                        StatusCodes.Status409Conflict,
                        ApiErrorMessages.OfferSoldOut,
                        $"{offer.Name} chỉ còn {offer.RemainingQuantity} suất, bạn đặt {quantity}.");
                }

                // Cộng dồn mọi đơn còn sống của khách trong đợt, kể cả đơn đã nhận (BR-06).
                var alreadyBought = _orders
                    .Where(o => o.CustomerId == customerId && o.Status is OrderStatus.PendingPayment or OrderStatus.Paid or OrderStatus.Redeemed)
                    .SelectMany(o => o.Items)
                    .Where(i => i.OfferId == offer.Id)
                    .Sum(i => i.Quantity);

                if (alreadyBought + quantity > offer.MaxPerCustomer)
                {
                    throw Error(
                        StatusCodes.Status409Conflict,
                        "PURCHASE_LIMIT_EXCEEDED",
                        $"Mỗi người chỉ mua tối đa {offer.MaxPerCustomer} suất \"{offer.Name}\" — bạn đã có {alreadyBought}.");
                }
            }

            foreach (var (offer, quantity) in lines)
            {
                offer.RemainingQuantity -= quantity;
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderCode = NewOrderCode(),
                CustomerId = customerId,
                Store = batch.Store,
                SaleBatchId = batch.SaleBatchId,
                PickupStartTime = batch.PickupStartTime,
                PickupEndTime = batch.PickupEndTime,
                PaymentDeadline = now.AddMinutes(PaymentMinutes),
                CreatedAt = now,
                Items = lines
                    .Select(l => new OrderItemDto
                    {
                        OfferId = l.Offer.Id,
                        OfferName = l.Offer.Name,
                        Quantity = l.Quantity,
                        UnitPrice = l.Offer.ActualPrice,
                        LineTotal = l.Offer.ActualPrice * l.Quantity,
                    })
                    .ToList(),
            };
            _orders.Add(order);

            return ToDto(order);
        }
    }

    public OrderPaymentDto Pay(Guid customerId, Guid orderId, PayOrderRequest request)
    {
        lock (_gate)
        {
            ExpireOverdueOrders();
            var order = FindOrder(customerId, orderId);

            if (order.Status != OrderStatus.PendingPayment)
            {
                throw Error(StatusCodes.Status409Conflict, "ORDER_NOT_PAYABLE", null);
            }

            if (request.Outcome == PaymentOutcome.Failure)
            {
                // Thất bại không huỷ đơn — tồn kho vẫn giữ tới hạn thanh toán (OQ-17).
                return new OrderPaymentDto { Order = ToDto(order) };
            }

            order.Status = OrderStatus.Paid;
            order.PaidAt = Now;
            order.QrToken = NewQrToken(order);

            return new OrderPaymentDto
            {
                Order = ToDto(order),
                QrTicket = new QrTokenDto { Token = order.QrToken, ExpiresAt = order.PickupEndTime },
            };
        }
    }

    public OrderDto Cancel(Guid customerId, Guid orderId)
    {
        lock (_gate)
        {
            ExpireOverdueOrders();
            var order = FindOrder(customerId, orderId);
            var now = Now;

            switch (order.Status)
            {
                case OrderStatus.Redeemed:
                    throw Error(StatusCodes.Status409Conflict, "ORDER_ALREADY_REDEEMED", null);
                case OrderStatus.Paid when now > CancelDeadline(order):
                case OrderStatus.Cancelled:
                case OrderStatus.NoShow:
                    throw Error(StatusCodes.Status409Conflict, "CANCEL_WINDOW_PASSED", null);
            }

            order.RefundStatus = order.Status == OrderStatus.Paid ? RefundStatus.Refunded : RefundStatus.None;
            MarkCancelled(order, CancelledBy.Customer, null);

            return ToDto(order);
        }
    }

    public QrTicketDto GetQrTicket(Guid customerId, Guid orderId)
    {
        lock (_gate)
        {
            var order = FindOrder(customerId, orderId);
            if (order.Status != OrderStatus.Paid || order.QrToken is null)
            {
                throw Error(StatusCodes.Status404NotFound, ApiErrorMessages.NotFound, "Đơn chưa thanh toán nên chưa có vé QR.");
            }

            return new QrTicketDto
            {
                OrderCode = order.OrderCode,
                Token = order.QrToken,
                ExpiresAt = order.PickupEndTime,
                PickupStartTime = order.PickupStartTime,
                StoreName = order.Store.Name,
            };
        }
    }

    public PagedResponse<OrderDto> GetMyOrders(Guid customerId, IDictionary<string, StringValues> query)
    {
        var status = Enum.TryParse<OrderStatus>(query.TryGetValue("status", out var value) ? value.ToString() : null, out var parsed)
            ? parsed
            : (OrderStatus?)null;

        lock (_gate)
        {
            ExpireOverdueOrders();
            var rows = _orders
                .Where(o => o.CustomerId == customerId && (status is null || o.Status == status))
                .OrderByDescending(o => o.CreatedAt)
                .Select(ToDto)
                .ToList();

            return Page(rows, ReadInt(query, "page") ?? 1, ReadInt(query, "pageSize") ?? 20);
        }
    }

    public OrderDto GetMyOrder(Guid customerId, Guid orderId)
    {
        lock (_gate)
        {
            ExpireOverdueOrders();
            return ToDto(FindOrder(customerId, orderId));
        }
    }

    // ---------- Sau khi nhận hàng (03 §5) ----------

    public object CreateReview(Guid customerId, Guid orderId, CreateReviewRequest request)
    {
        lock (_gate)
        {
            var order = FindOrder(customerId, orderId);
            RequireRedeemed(order, "đánh giá");

            if (order.Reviewed)
            {
                throw Error(StatusCodes.Status409Conflict, "REVIEW_ALREADY_EXISTS", null);
            }

            order.Reviewed = true;
            return new { id = Guid.NewGuid(), orderId, request.Rating, request.Comment, request.PhotoUrl, createdAt = Now };
        }
    }

    public object CreateDispute(Guid customerId, Guid orderId, CreateDisputeRequest request)
    {
        lock (_gate)
        {
            var order = FindOrder(customerId, orderId);
            RequireRedeemed(order, "khiếu nại");

            if (order.Disputed)
            {
                throw Error(StatusCodes.Status409Conflict, "DISPUTE_ALREADY_EXISTS", null);
            }

            if (Now > order.RedeemedAt!.Value.AddHours(2))
            {
                throw Error(StatusCodes.Status409Conflict, "DISPUTE_WINDOW_PASSED", null);
            }

            order.Disputed = true;
            return new { id = Guid.NewGuid(), orderId, request.Reason, request.Description, request.PhotoUrl, status = "Open", createdAt = Now };
        }
    }

    // ---------- Nội bộ ----------

    private AuthResponse IssueTokens(Account account)
    {
        var refreshToken = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        _refreshTokens[refreshToken] = account.Id;

        return new AuthResponse
        {
            AccessToken = $"mock-access-{Guid.NewGuid():N}",
            RefreshToken = refreshToken,
            ExpiresAt = Now.Add(_accessTokenLifetime),
            User = ToUserInfo(account),
        };
    }

    private static UserInfo ToUserInfo(Account account) => new()
    {
        Id = account.Id,
        FullName = account.FullName,
        Email = account.Email,
        Roles = account.Roles,
        BrandId = account.BrandId,
        StoreId = account.StoreId,
    };

    private Offer FindOffer(Guid offerId)
        => _offers.FirstOrDefault(o => o.Id == offerId)
            ?? throw Error(StatusCodes.Status404NotFound, ApiErrorMessages.NotFound, "Không tìm thấy mặt hàng.");

    // Đơn của người khác cũng trả 404, không trả 403 — không để lộ là đơn có tồn tại (03 §4.5).
    private Order FindOrder(Guid customerId, Guid orderId)
        => _orders.FirstOrDefault(o => o.Id == orderId && o.CustomerId == customerId)
            ?? throw Error(StatusCodes.Status404NotFound, ApiErrorMessages.NotFound, "Không tìm thấy đơn của bạn.");

    private static void RequireRedeemed(Order order, string action)
    {
        if (order.Status != OrderStatus.Redeemed || order.RedeemedAt is null)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ApiErrorMessages.ValidationFailed,
                $"Chỉ {action} được đơn đã nhận hàng.");
        }
    }

    /// <summary>BR-13: min(paidAt + 15', pickupStart − 5').</summary>
    private static DateTime CancelDeadline(Order order)
    {
        var byPayment = order.PaidAt!.Value.AddMinutes(15);
        var byPickup = order.PickupStartTime.AddMinutes(-5);
        return byPayment < byPickup ? byPayment : byPickup;
    }

    // Thay cho OrderExpiryService của API: đơn quá hạn thanh toán bị huỷ và trả tồn kho.
    private void ExpireOverdueOrders()
    {
        var now = Now;
        foreach (var order in _orders.Where(o => o.Status == OrderStatus.PendingPayment && o.PaymentDeadline < now))
        {
            MarkCancelled(order, CancelledBy.System, "Quá hạn thanh toán");
        }
    }

    private void MarkCancelled(Order order, CancelledBy by, string? reason)
    {
        foreach (var item in order.Items)
        {
            var offer = _offers.FirstOrDefault(o => o.Id == item.OfferId);
            if (offer is not null)
            {
                offer.RemainingQuantity += item.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = by;
        order.CancelReason = reason;
        order.CancelledAt = Now;
        order.QrToken = null;
    }

    private static string NewOrderCode()
        => "FR-" + string.Create(8, 0, (chars, _) =>
        {
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = OrderCodeAlphabet[Random.Shared.Next(OrderCodeAlphabet.Length)];
            }
        });

    // Chỉ mô phỏng hình dạng token thật (dữ liệu + chữ ký) — không ký thật, QrService mới giữ khoá (OQ-22).
    private static string NewQrToken(Order order)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            orderId = order.Id,
            storeId = order.Store.Id,
            exp = new DateTimeOffset(order.PickupEndTime).ToUnixTimeSeconds(),
        });
        var signature = Guid.NewGuid().ToByteArray();
        return $"{Base64Url(payload)}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static ActiveOfferDto ToDto(Offer offer, double? distanceKm) => new()
    {
        Id = offer.Id,
        SaleBatchId = offer.SaleBatchId,
        StoreId = offer.Store.Id,
        StoreName = offer.Store.Name,
        DistanceKm = distanceKm is null ? null : Math.Round(distanceKm.Value, 2),
        Name = offer.Name,
        Description = offer.Description,
        ImageUrl = null,
        CategoryName = offer.Category.Name,
        OfferType = offer.OfferType,
        OriginalPrice = offer.OriginalPrice,
        ActualPrice = offer.ActualPrice,
        DiscountPercent = Math.Round((double)(1 - (offer.ActualPrice / offer.OriginalPrice)) * 100, 1),
        RemainingQuantity = offer.RemainingQuantity,
        MaxPerCustomer = offer.MaxPerCustomer,
        SaleStartTime = offer.SaleStartTime,
        SaleEndTime = offer.SaleEndTime,
        PickupStartTime = offer.PickupStartTime,
        PickupEndTime = offer.PickupEndTime,
    };

    private static OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        OrderCode = order.OrderCode,
        Status = order.Status,
        StoreId = order.Store.Id,
        StoreName = order.Store.Name,
        StoreAddress = order.Store.Address,
        SaleBatchId = order.SaleBatchId,
        PickupStartTime = order.PickupStartTime,
        PickupEndTime = order.PickupEndTime,
        PaymentDeadline = order.PaymentDeadline,
        TotalAmount = order.Items.Sum(i => i.LineTotal),
        PaidAt = order.PaidAt,
        CancelledBy = order.CancelledBy,
        CancelReason = order.CancelReason,
        CancelledAt = order.CancelledAt,
        RefundStatus = order.RefundStatus,
        Items = order.Items,
        CreatedAt = order.CreatedAt,
    };

    private static PagedResponse<T> Page<T>(List<T> rows, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, PagedResponse<T>.MaxPageSize);
        page = Math.Max(page, 1);

        return new PagedResponse<T>
        {
            Items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = rows.Count,
            TotalPages = (int)Math.Ceiling(rows.Count / (double)pageSize),
        };
    }

    private static double DistanceKm(double lat, double lng, Store store)
    {
        // Haversine, như API tính ở 02-DOMAIN-MODEL.md §5.1.
        const double earthRadiusKm = 6371;
        static double Rad(double degrees) => degrees * Math.PI / 180;

        var dLat = Rad(store.Latitude - lat);
        var dLng = Rad(store.Longitude - lng);
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
            + (Math.Cos(Rad(lat)) * Math.Cos(Rad(store.Latitude)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2));
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static Guid? ReadGuid(IDictionary<string, StringValues> query, string key)
        => query.TryGetValue(key, out var value) && Guid.TryParse(value, out var id) ? id : null;

    private static int? ReadInt(IDictionary<string, StringValues> query, string key)
        => query.TryGetValue(key, out var value) && int.TryParse(value, out var number) ? number : null;

    private static double? ReadDouble(IDictionary<string, StringValues> query, string key)
        => query.TryGetValue(key, out var value)
            && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;

    private static ApiException Error(int status, string errorCode, string? detail)
        => new(status, errorCode, ApiErrorMessages.Resolve(status, errorCode, detail));

    private void Seed()
    {
        var now = Now;

        var customer = new Account(Guid.NewGuid(), "Nguyễn Văn An", "khach@demo.vn", DemoPassword, [AppRoles.Customer]);
        _accounts.Add(customer);

        string[] categoryNames = ["Bánh ngọt", "Đồ mặn", "Đồ chay", "Cơm & sushi", "Đồ uống"];
        _categories.AddRange(categoryNames.Select(name => new CategoryDto { Id = Guid.NewGuid(), Name = name }));
        CategoryDto Category(string name) => _categories.First(c => c.Name == name);

        var bakery = new Store(Guid.NewGuid(), "Tiệm bánh Mây — Nguyễn Huệ", "12 Nguyễn Huệ, Bến Nghé, Quận 1", 10.774100, 106.703500);
        var mart = new Store(Guid.NewGuid(), "GreenMart — Lê Lợi", "45 Lê Lợi, Bến Thành, Quận 1", 10.772400, 106.698900);
        var vegan = new Store(Guid.NewGuid(), "Bếp Chay An Nhiên", "88 Võ Văn Tần, Phường 6, Quận 3", 10.781800, 106.690200);
        var sushi = new Store(Guid.NewGuid(), "Sushi Góc Phố", "21 Nguyễn Hữu Cảnh, Phường 22, Bình Thạnh", 10.795000, 106.721800);
        _stores.AddRange([bakery, mart, vegan, sushi]);

        var brandId = Guid.NewGuid();
        _accounts.Add(new Account(Guid.NewGuid(), "Trần Thị Bình", "nhanvien@demo.vn", DemoPassword, [AppRoles.StoreStaff], brandId, bakery.Id));
        _accounts.Add(new Account(Guid.NewGuid(), "Lê Minh Châu", "thuonghieu@demo.vn", DemoPassword, [AppRoles.BrandManager], brandId));
        _accounts.Add(new Account(Guid.NewGuid(), "Quản trị viên", "admin@demo.vn", DemoPassword, [AppRoles.Admin]));

        // Mọi đợt bán đang mở tính từ lúc ứng dụng khởi động, để mở lên là có hàng để mua.
        void Batch(Store store, int pickupInMinutes, params (string Name, string Category, OfferType Type, decimal Original, decimal Actual, int Remaining, string Description)[] items)
        {
            var batchId = Guid.NewGuid();
            foreach (var item in items)
            {
                _offers.Add(new Offer
                {
                    Id = Guid.NewGuid(),
                    SaleBatchId = batchId,
                    Store = store,
                    Category = Category(item.Category),
                    Name = item.Name,
                    Description = item.Description,
                    OfferType = item.Type,
                    OriginalPrice = item.Original,
                    ActualPrice = item.Actual,
                    RemainingQuantity = item.Remaining,
                    MaxPerCustomer = 3,
                    SaleStartTime = now.AddMinutes(-60),
                    SaleEndTime = now.AddMinutes(pickupInMinutes + 30),
                    PickupStartTime = now.AddMinutes(pickupInMinutes),
                    PickupEndTime = now.AddMinutes(pickupInMinutes + 75),
                });
            }
        }

        Batch(
            bakery,
            60,
            ("Túi mù bánh ngọt", "Bánh ngọt", OfferType.SurpriseBag, 120000, 35000, 4, "3–5 món bánh ngọt trong ngày: croissant, bánh bông lan, tart trứng…"),
            ("Túi mù bánh mì mặn", "Đồ mặn", OfferType.SurpriseBag, 100000, 30000, 6, "Bánh mì xúc xích, bánh mì phô mai, bánh pizza mini."),
            ("Bánh mousse chanh dây", "Bánh ngọt", OfferType.ClearanceItem, 55000, 29000, 0, "Mousse chanh dây cỡ cá nhân, làm sáng nay."));
        Batch(
            mart,
            45,
            ("Cơm nắm cá ngừ mayo", "Cơm & sushi", OfferType.ClearanceItem, 25000, 13000, 12, "Hạn dùng hết hôm nay."),
            ("Sandwich gà nướng", "Đồ mặn", OfferType.ClearanceItem, 39000, 21000, 8, "Sandwich gà nướng mật ong, rau xà lách."),
            ("Sữa chua trái cây", "Đồ uống", OfferType.ClearanceItem, 22000, 12000, 15, "Hộp 150g, giữ lạnh."));
        Batch(
            vegan,
            90,
            ("Túi mù đồ chay", "Đồ chay", OfferType.SurpriseBag, 90000, 27000, 5, "2–3 món chay của bếp trong ngày: cơm, bún, chả nấm…"));
        Batch(
            sushi,
            120,
            ("Combo sushi cuối ngày", "Cơm & sushi", OfferType.ClearanceItem, 160000, 85000, 3, "12 miếng sushi tổng hợp."),
            ("Túi mù sushi & cơm cuộn", "Cơm & sushi", OfferType.SurpriseBag, 150000, 45000, 2, "Ngẫu nhiên sushi, maki, cơm cuộn còn lại cuối ca."));

        // Hai đơn cũ để màn lịch sử, đánh giá và khiếu nại có dữ liệu ngay.
        var pastBatch = Guid.NewGuid();
        _orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = "FR-8A3K9MQ2",
            CustomerId = customer.Id,
            Store = bakery,
            SaleBatchId = pastBatch,
            Status = OrderStatus.Redeemed,
            PickupStartTime = now.AddMinutes(-90),
            PickupEndTime = now.AddMinutes(-15),
            PaymentDeadline = now.AddMinutes(-140),
            PaidAt = now.AddMinutes(-145),
            RedeemedAt = now.AddMinutes(-30),
            CreatedAt = now.AddMinutes(-150),
            Items = [new OrderItemDto { OfferId = Guid.NewGuid(), OfferName = "Túi mù bánh ngọt", Quantity = 2, UnitPrice = 35000, LineTotal = 70000 }],
        });
        _orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = "FR-Q7WZ4HTP",
            CustomerId = customer.Id,
            Store = sushi,
            SaleBatchId = Guid.NewGuid(),
            Status = OrderStatus.Cancelled,
            CancelledBy = CancelledBy.Store,
            CancelReason = "Tủ lạnh hỏng, không bảo đảm chất lượng sushi.",
            RefundStatus = RefundStatus.Refunded,
            PickupStartTime = now.AddDays(-1).AddMinutes(60),
            PickupEndTime = now.AddDays(-1).AddMinutes(120),
            PaymentDeadline = now.AddDays(-1).AddMinutes(10),
            PaidAt = now.AddDays(-1).AddMinutes(5),
            CancelledAt = now.AddDays(-1).AddMinutes(30),
            CreatedAt = now.AddDays(-1),
            Items = [new OrderItemDto { OfferId = Guid.NewGuid(), OfferName = "Combo sushi cuối ngày", Quantity = 1, UnitPrice = 85000, LineTotal = 85000 }],
        });
    }

    private sealed record Account(
        Guid Id,
        string FullName,
        string Email,
        string Password,
        IReadOnlyList<string> Roles,
        Guid? BrandId = null,
        Guid? StoreId = null);

    private sealed record Store(Guid Id, string Name, string Address, double Latitude, double Longitude);

    private sealed class Offer
    {
        public Guid Id { get; init; }
        public Guid SaleBatchId { get; init; }
        public required Store Store { get; init; }
        public required CategoryDto Category { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public OfferType OfferType { get; init; }
        public decimal OriginalPrice { get; init; }
        public decimal ActualPrice { get; init; }
        public int RemainingQuantity { get; set; }
        public int MaxPerCustomer { get; init; }
        public DateTime SaleStartTime { get; init; }
        public DateTime SaleEndTime { get; init; }
        public DateTime PickupStartTime { get; init; }
        public DateTime PickupEndTime { get; init; }
    }

    private sealed class Order
    {
        public Guid Id { get; init; }
        public required string OrderCode { get; init; }
        public Guid CustomerId { get; init; }
        public required Store Store { get; init; }
        public Guid SaleBatchId { get; init; }
        public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
        public DateTime PickupStartTime { get; init; }
        public DateTime PickupEndTime { get; init; }
        public DateTime PaymentDeadline { get; init; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public CancelledBy? CancelledBy { get; set; }
        public string? CancelReason { get; set; }
        public RefundStatus RefundStatus { get; set; }
        public string? QrToken { get; set; }
        public bool Reviewed { get; set; }
        public bool Disputed { get; set; }
        public DateTime CreatedAt { get; init; }
        public List<OrderItemDto> Items { get; init; } = [];
    }
}
