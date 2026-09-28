# FreshRescue — Chuẩn code & tính đồng nhất

> Luật hành vi & Git: `05-TEAM-RULES.md` · Contract: `03-API-CONTRACT.md`
> Cập nhật: 2026-09-17
>
> `05-TEAM-RULES.md` trả lời **"được làm gì"**. File này trả lời **"viết thế nào"** —
> để code của 6 người đọc lên như một người viết.

---

## 1. Nguyên tắc: máy ép, không dựa trí nhớ

Sáu sinh viên sẽ không nhớ 40 quy ước. Nên hầu hết quy ước ở đây **không nằm trong tài
liệu này** — chúng nằm trong các file cấu hình mà công cụ tự áp dụng.

| File | Ép cái gì | Ai áp dụng |
| --- | --- | --- |
| `.editorconfig` | Thụt lề, ngoặc nhọn, `using`, **quy tắc đặt tên** | Visual Studio, VS Code, `dotnet format` |
| `.gitattributes` | Ký tự xuống dòng CRLF | Git, mọi lần commit |
| `Directory.Build.props` | TFM, `Nullable`, `LangVersion`, mức phân tích mã | MSBuild, mọi project |
| `Directory.Packages.props` | **Phiên bản NuGet dùng chung** | NuGet, mọi restore |
| `global.json` | Phiên bản SDK | `dotnet` CLI |
| `.gitmessage` | Khuôn commit | Git, mỗi lần commit |
| `.github/pull_request_template.md` | Khuôn PR | GitHub |

**Không chỉnh style trong Options của riêng máy mình.** Thấy quy ước sai thì sửa
`.editorconfig` và mở PR — đổi cho cả nhóm, hoặc không đổi gì cả.

### 1.1. Vì sao ghim `AnalysisLevel` = 8.0

Máy trong nhóm đang có SDK 9 và 10, không ai có SDK 8. Điều đó **vẫn build được
`net8.0`** vì runtime 8.0.27 và reference pack 8.0.x đã cài sẵn — nên `global.json` ghim
`8.0.400` kèm `rollForward: latestMajor`, chấp nhận cả SDK 8, 9 và 10.

Nhưng mỗi SDK bật thêm một bộ cảnh báo khác nhau. Không ghim `AnalysisLevel` thì cùng
một dòng code sẽ "sạch" trên máy này và "có 3 cảnh báo" trên máy kia — đúng kiểu không
đồng nhất khó chịu nhất vì không ai sai cả.

---

## 2. Cài một lần trên máy mỗi người

```bash
# 1. Dùng khuôn commit chung
git config commit.template .gitmessage

# 2. Git không tự đổi ký tự xuống dòng (đã có .gitattributes lo)
git config core.autocrlf false

# 3. Luôn rebase khi pull
git config pull.rebase true

# 4. Công cụ định dạng
dotnet tool install -g dotnet-format   # nếu SDK chưa kèm sẵn
```

**Trước mỗi lần commit:**

```bash
dotnet format --verify-no-changes    # báo lỗi = chạy `dotnet format` rồi commit lại
dotnet build
```

---

## 3. Cấu trúc thư mục — nơi đặt file là một phần của quy ước

Đặt file sai chỗ cũng là một dạng không đồng nhất, và khó sửa hơn style vì phải đổi
namespace.

```
FreshRescue.Entities/
├─ Common/        BaseEntity, IAuditable, ISoftDelete
├─ Enums/         OrderStatus, OfferType, SaleBatchStatus…
├─ Identity/      AppUser, AppRole, RefreshToken
├─ Catalog/       Brand, Store, StoreStaff, Category, OfferTemplate
├─ Sales/         SaleBatch, Offer
├─ Ordering/      Order, OrderItem, QrTicket, Payment
└─ Engagement/    Review, Dispute, FavoriteStore, Notification

FreshRescue.DataAccess/
├─ FreshRescueDbContext.cs
├─ Configurations/   MỘT file cho MỘT entity: StoreConfiguration.cs
├─ Repositories/     IGenericRepository<T>, GenericRepository<T>, IOfferRepository…
├─ UnitOfWork/       IUnitOfWork, UnitOfWork
├─ Migrations/       (chỉ Tùng đụng vào)
├─ Seeding/          DbSeeder
└─ DependencyInjection.cs

FreshRescue.Business/
├─ Common/       AppException, ErrorCodes, PagedResponse<T>, CurrentUser
├─ Features/     ← chia theo LÁT DỌC, khớp với sở hữu ở 05-TEAM-RULES.md §5.1
│  ├─ Auth/          Dtos/  Validators/  IAuthService.cs  AuthService.cs
│  ├─ Catalog/
│  ├─ Sales/
│  ├─ Ordering/
│  ├─ Redemption/
│  └─ Admin/
├─ Mapping/      MỘT profile cho MỘT feature: OrderingProfile.cs
└─ DependencyInjection.cs

FreshRescue.API/
├─ Controllers/       OrdersController.cs…
├─ Hubs/              OfferHub.cs
├─ Middleware/        ExceptionHandlingMiddleware.cs
├─ BackgroundServices/
├─ Extensions/        AddJwtAuth, AddSwaggerDocs, AddODataConfig
└─ Program.cs
```

> `Business/Features/` chia theo lát dọc **cố ý trùng với bảng phân công**. Mỗi người có
> một thư mục của mình, nên xung đột merge gần như bằng không — hai người hiếm khi sửa
> cùng một file.

---

## 4. Khuôn mẫu code

Copy đúng những khuôn này. Đừng sáng tạo thêm kiểu thứ bảy.

### 4.1. Controller

```csharp
namespace FreshRescue.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Đặt hàng và giữ chỗ tồn kho.</summary>
    /// <response code="201">Tạo đơn thành công</response>
    /// <response code="409">Hết suất hoặc vượt giới hạn mua</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = order.Id }, order);
    }
}
```

Bắt buộc: `[ApiController]` · route viết tay dạng kebab-case · `[ProducesResponseType]`
đủ mã · XML comment (Swagger đọc) · `CancellationToken` · **không** `try/catch` ·
**không** `if` nghiệp vụ · **không** `DbContext`.

### 4.2. Service

```csharp
namespace FreshRescue.Business.Features.Ordering;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct);
}

public class OrderService : IOrderService
{
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

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var batch = await _uow.SaleBatches.GetByIdAsync(request.SaleBatchId, ct)
            ?? throw AppException.NotFound(ErrorCodes.NotFound, "Không tìm thấy đợt bán.");

        if (batch.Status != SaleBatchStatus.Active || now < batch.SaleStartTime || now > batch.SaleEndTime)
        {
            throw AppException.Conflict(ErrorCodes.SaleWindowClosed, "Đợt bán đã kết thúc.");
        }

        // ... BR-01, BR-03, BR-06 ...

        await _uow.SaveChangesAsync(ct);
        return _mapper.Map<OrderDto>(order);
    }
}
```

Bắt buộc: interface cùng file hoặc cùng thư mục · phụ thuộc tiêm qua constructor,
`readonly` · **`TimeProvider` thay cho `DateTime.UtcNow`** · ném `AppException` chứ
không trả về mã lỗi · **một** `SaveChangesAsync` cho mỗi request.

> **Vì sao `TimeProvider`.** Hệ thống này dày đặc thời gian — 4 mốc đợt bán, hạn thanh
> toán, hạn QR, hạn khiếu nại 2 giờ. Gọi thẳng `DateTime.UtcNow` thì muốn test "đơn quá
> hạn" phải ngồi chờ 10 phút thật. `TimeProvider` có sẵn trong .NET 8, test tua thời
> gian được. Đăng ký một dòng: `services.AddSingleton(TimeProvider.System)`.

### 4.3. DTO & Request

Mỗi thứ một file, đặt trong `Features/<Feature>/Dtos/`.

```csharp
public record CreateOrderRequest(Guid SaleBatchId, List<CreateOrderItemRequest> Items);
public record CreateOrderItemRequest(Guid OfferId, int Quantity);

public record OrderDto
{
    public Guid Id { get; init; }
    public string OrderCode { get; init; } = default!;
    public OrderStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
}
```

`record` cho DTO. `Request` cho dữ liệu vào, `Dto` cho dữ liệu ra
(`03-API-CONTRACT.md`). **Không bao giờ** trả entity ra ngoài Controller.

### 4.4. Validator — FluentValidation, không DataAnnotations

```csharp
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.SaleBatchId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Đơn hàng phải có ít nhất một món.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10);
        });
    }
}
```

Chỉ kiểm **định dạng và khoảng giá trị** ở đây. Quy tắc nghiệp vụ (`BR-xx`) nằm ở
service — xem `05-TEAM-RULES.md` §5.7.

### 4.5. Mapping — AutoMapper, không map tay

```csharp
public class OrderingProfile : Profile
{
    public OrderingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.Store.Name));
    }
}
```

### 4.6. Đăng ký DI — một extension method cho mỗi tầng

```csharp
// FreshRescue.Business/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
```

`Program.cs` chỉ gọi `builder.Services.AddDataAccessLayer(cs).AddBusinessLayer().AddApiLayer();`
— **không** có 40 dòng `AddScoped` trong `Program.cs`.

---

## 5. Xử lý lỗi — một đường duy nhất

Đây là chỗ dễ lệch nhất: không thống nhất thì 6 người sẽ trả lỗi 6 kiểu, và Web Client
phải viết 6 nhánh xử lý.

```csharp
// FreshRescue.Business/Common/ErrorCodes.cs
public static class ErrorCodes
{
    public const string ValidationFailed      = "VALIDATION_FAILED";
    public const string NotFound              = "NOT_FOUND";
    public const string ForbiddenScope        = "FORBIDDEN_SCOPE";
    public const string SaleWindowClosed      = "SALE_WINDOW_CLOSED";
    public const string OfferSoldOut          = "OFFER_SOLD_OUT";
    public const string PurchaseLimitExceeded = "PURCHASE_LIMIT_EXCEEDED";
    public const string OrderAlreadyRedeemed  = "ORDER_ALREADY_REDEEMED";
    // … đủ 28 mã, đúng theo 03-API-CONTRACT.md §1.4
}

// FreshRescue.Business/Common/AppException.cs
public class AppException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    private AppException(string errorCode, int statusCode, string detail) : base(detail)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public static AppException BadRequest(string code, string detail) => new(code, 400, detail);
    public static AppException Forbidden(string code, string detail)  => new(code, 403, detail);
    public static AppException NotFound(string code, string detail)   => new(code, 404, detail);
    public static AppException Conflict(string code, string detail)   => new(code, 409, detail);
}
```

Service **ném** `AppException`. Middleware bắt và dịch sang `ProblemDetails` đúng khuôn
`03-API-CONTRACT.md` §1.3. Controller không biết gì về chuyện này.

```csharp
// ✅ đúng
throw AppException.Conflict(ErrorCodes.OfferSoldOut, $"'{offer.Name}' chỉ còn {offer.RemainingQuantity} suất.");

// ❌ sai — 6 kiểu trả lỗi khác nhau sẽ mọc ra từ đây
return BadRequest("Hết hàng");
return StatusCode(409, new { message = "sold out" });
return NotFound();
throw new Exception("het hang");
```

---

## 6. Đặt tên endpoint

| Quy ước | Ví dụ |
| --- | --- |
| Danh từ **số nhiều**, **kebab-case** | `/api/v1/sale-batches` |
| Hành động = tài nguyên con, dùng `POST` | `POST /api/v1/orders/{id}/cancel` |
| Không có động từ trong đường dẫn tài nguyên | ❌ `/api/v1/getOrders` |
| Nhóm theo vai trò khi phân quyền khác nhau | `/staff/...` `/brand/...` `/admin/...` |

Route viết tay bằng chuỗi, **không** dùng `[controller]` — đổi tên class sẽ âm thầm đổi
URL và phá contract.

---

## 7. Một việc — một cách

Bảng này tồn tại để chấm dứt tranh luận. Cột phải là lựa chọn duy nhất.

| Việc | Cách duy nhất | Cấm |
| --- | --- | --- |
| Lấy thời gian hiện tại | `TimeProvider` | `DateTime.Now`, `DateTime.UtcNow` |
| Sinh khoá chính | `Guid.NewGuid()` ở tầng ứng dụng | Để CSDL sinh |
| Trả lỗi | `throw AppException` | `return BadRequest(...)` |
| Kiểm tra dữ liệu vào | FluentValidation | DataAnnotations |
| Chuyển entity → DTO | AutoMapper profile | Map tay, constructor nhận entity |
| Truy cập CSDL từ service | `IUnitOfWork` | Tiêm thẳng `DbContext` |
| Lưu thay đổi | Một `SaveChangesAsync` cuối mỗi request | Gọi rải rác nhiều lần |
| Ghi log | `ILogger<T>` + placeholder có tên | `Console.WriteLine`, nối chuỗi |
| Enum ra JSON | Chuỗi (`JsonStringEnumConverter`) | Số |
| Danh sách trả về | `PagedResponse<T>` | `List<T>` trần |
| Hằng số role | `AppRoles.Customer` | Chuỗi `"Customer"` rải trong code |
| Bất đồng bộ | `async Task` + `CancellationToken` | `.Result`, `.Wait()`, `async void` |

```csharp
// ✅ log có cấu trúc
_logger.LogWarning("Đặt hàng thất bại do hết suất. OfferId={OfferId}, Yêu cầu={Qty}", offerId, qty);

// ❌
_logger.LogWarning("Het suat offer " + offerId);
```

---

## 8. Trước khi mở PR

```bash
dotnet format --verify-no-changes
dotnet build
dotnet test
```

Ba lệnh này là cổng kiểm tra thật. Checklist trong
`.github/pull_request_template.md` nhắc lại đúng ba lệnh đó.

> Nếu `dotnet format` sửa hàng trăm file không phải của bạn: **dừng lại, gọi Tùng.**
> Dấu hiệu là ai đó commit trước khi `.editorconfig` có hiệu lực, cần một PR dọn dẹp
> riêng — đừng trộn việc định dạng lại toàn bộ vào PR tính năng, review sẽ thành bất khả.
