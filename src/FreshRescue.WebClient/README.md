# FreshRescue.WebClient

ASP.NET Core MVC, chỉ nói chuyện với `FreshRescue.API` qua HTTP. Chủ sở hữu hạ tầng: **Đức Anh**
(`docs/07-WORK-BREAKDOWN.md` §12.3).

| Thư mục | Ai sở hữu |
| --- | --- |
| `Areas/Customer/` | Đức Anh — storefront, Quick Checkout, đơn của tôi, vé QR, đánh giá, khiếu nại |
| `Areas/Dashboard/` | Hoàng Huy |
| `Services/`, `Extensions/`, `Filters/`, `Program.cs`, `Views/Shared/_Layout.cshtml` | Đức Anh — **đóng băng**, cần đổi thì nhắn trước |

## Chạy

```bash
dotnet run --project src/FreshRescue.WebClient     # https://localhost:7002
```

Mặc định `"UseMockApi": true` (appsettings.json): **không cần API, không cần SQL Server**. Tài khoản
demo, mật khẩu đều là `123456`:

| Email | Vai trò |
| --- | --- |
| `khach@demo.vn` | Customer — có sẵn một đơn đã nhận và một đơn bị huỷ |
| `nhanvien@demo.vn` | StoreStaff |
| `thuonghieu@demo.vn` | BrandManager |
| `admin@demo.vn` | Admin |

Gọi API thật: chạy `FreshRescue.API` (cổng 7001), rồi đặt `"UseMockApi": false` trong
`appsettings.Development.json` của bạn (file này không commit), hoặc:

```bash
UseMockApi=false dotnet run --project src/FreshRescue.WebClient
```

## Gọi API — dùng `IApiClient`, đừng tự new `HttpClient`

```csharp
public class SaleBatchesController : Controller
{
    private readonly IApiClient _api;
    public SaleBatchesController(IApiClient api) => _api = api;

    public async Task<IActionResult> IndexAsync(CancellationToken cancellationToken)
    {
        var batches = await _api.GetAsync<PagedResponse<SaleBatchDto>>("staff/sale-batches?page=1", cancellationToken);
        return View(batches);
    }
}
```

- Đường dẫn tương đối với `/api/v1/`, không có `/` đầu. Ghép query bằng `QueryHelpers.AddQueryString`.
- Token tự gắn và tự đổi khi hết hạn. Đăng nhập là cookie, trong cookie có JWT, nên
  `[Authorize(Roles = AppRoles.StoreStaff)]` và `User.FindFirstValue("store_id")` dùng được ngay.
- Lỗi API ném `ApiException` (`Status`, `ErrorCode`, `Message`, `Errors`). Chỉ bắt khi muốn hiện lỗi
  ngay trên form, và bắt theo `ErrorCode`. Lỗi không bắt thì `ApiExceptionFilter` lo: 401 về trang
  đăng nhập, request AJAX nhận JSON, còn lại hiện trang lỗi.
- Model của API đặt ở `Models/Api/`, tên trường chép đúng `docs/03-API-CONTRACT.md`.
- Endpoint mới chạy ở chế độ mock: thêm một nhánh vào `MockApiClient.Route` và dữ liệu vào
  `Services/Mock/MockApiStore.cs`. Nhánh chưa có thì mock trả `404` kèm tên endpoint còn thiếu.

## JS dùng chung — `window.FR` (`wwwroot/js/site.js`)

| Hàm | Dùng để |
| --- | --- |
| `FR.api(url, { method, body })` | `fetch` tới controller của WebClient, tự gửi CSRF token, 401 thì về trang đăng nhập |
| `FR.formatMoney`, `FR.formatTime`, `FR.formatWindow` | Hiển thị tiền VND và giờ Việt Nam từ chuỗi UTC |
| `FR.renderQr(element, token)` | Vẽ QR (cần `~/lib/qrcode-generator/qrcode.min.js`) |
| `data-countdown="<ISO UTC>"` | Đồng hồ đếm ngược, hết giờ bắn sự kiện `fr:expired` |

Phía C#, `Helpers/Display.cs` làm cùng việc định dạng cho Razor.
