# FreshRescue — Phân công công việc PRN232

> Kế hoạch tổng: `00-PLAN.md` · Yêu cầu: `01-REQUIREMENTS.md` · Contract: `03-API-CONTRACT.md`
> Cập nhật: 2026-09-28 — bỏ việc phục vụ PRM393 (`00-PLAN.md` §0) · Nộp **20/10/2026** (cuối Tuần 6)
>
> Mã việc dạng `T<tuần>-<người>-<số>`. Dùng mã này trong tên nhánh và mô tả PR.

---

## 1. Sở hữu lát dọc

Mỗi người sở hữu **trọn** controller + service + DTO của lát mình
(`05-TEAM-RULES.md` §5.1). Chia theo lát dọc chứ không theo tầng, để hai người hiếm khi
sửa cùng một file.

| Người | Lát dọc sở hữu | Thư mục chính |
| --- | --- | --- |
| **Tùng** *(lead)* | **Nền móng** (Entities, DbContext, Migrations, Repository/UoW, Middleware) + **Đặt hàng & Đối soát** (order, payment, cancel, cấp & quét QR) | `Entities/`, `DataAccess/`, `Business/Features/Ordering/` |
| **Hiếu** | **Xác thực** (Identity, JWT, claims, policy) + **Tiến trình nền** + **SignalR** + Thông báo | `Business/Features/Auth/`, `API/BackgroundServices/`, `API/Hubs/` |
| **TV3** | **Danh mục & Quản trị** (Brand, Store, Category, OfferTemplate, duyệt brand, người dùng, khiếu nại) + **OData** + Seeder | `Business/Features/Catalog/`, `Features/Admin/` |
| **TV4** | **Web Client — phía khách** (storefront, Quick Checkout, màn QR, lịch sử đơn, đánh giá) **+ hạ tầng WebClient dùng chung** | `WebClient/Areas/Customer/`, `WebClient/Services/` |
| **TV5** | **Web Client — bảng điều khiển** (Admin/Brand/Staff) + AJAX/SignalR JS + quét QR webcam | `WebClient/Areas/Dashboard/` |
| **TV6** | **Đợt bán** (SaleBatch, Offer) + **gRPC QrService** + Swagger/Postman + Load test | `Business/Features/Sales/`, `QrService/` |

> **Thay đổi so với `00-PLAN.md` §5:** Hiếu chuyển từ *Đợt bán* sang *Xác thực*, TV6 nhận
> *Đợt bán*. Lý do: Tùng không thể vừa làm nền móng vừa làm xác thực trong Tuần 3 —
> hai việc đó cộng lại vượt xa sức một người trong một tuần, và cả hai đều chặn người khác.

Chia theo lát dọc mới chỉ giải quyết xung đột ở **file nghiệp vụ**. Còn **file hạ tầng
dùng chung** và **quyền ghi chéo lát** — đọc **§12 trước khi bắt đầu Tuần 3**.

---

## 2. Sức người & khối lượng

| Người | pd/tuần | T2 | T3 | T4 | T5 | T6 | **Tổng** |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Tùng | 3.5 | 2.0 | 3.75 | 4.5 | 3.0 | 2.0 | **15.25** |
| Hiếu | 3.0 | 1.0 | 3.0 | 3.0 | 2.75 | 3.0 | **12.75** |
| TV3 | 2.5 | 1.0 | 2.5 | 2.5 | 2.5 | 2.0 | **10.5** |
| TV4 | 2.5 | 1.0 | 2.5 | 2.5 | 2.5 | 2.0 | **10.5** |
| TV5 | 2.5 | 1.0 | 2.5 | 2.5 | 2.5 | 2.0 | **10.5** |
| TV6 | 2.0 | 0.5 | 1.5 | 2.0 | 2.0 | 2.5 | **8.5** |
| | | **6.5** | **15.75** | **17.0** | **15.25** | **13.5** | **68.0** |

Phạm vi tính năng là **~61.5 pd** (`00-PLAN.md` §1.2, sau khi bỏ PRM393). Phân công
**68 pd** — phần chênh ~6.5 pd là quỹ cho sửa lỗi và làm lại, thứ không nằm trong ước
lượng tính năng.

Tùng gánh nhiều nhất vì ôm cả nền móng lẫn phần khó nhất. Bù lại, phần lớn công của Tùng
dồn vào Tuần 2–4; từ Tuần 5 chuyển sang review và hỗ trợ.

**Tuần 4 là tuần nặng nhất (17 pd).** Đừng xếp thêm gì vào đó.

---

## 3. Đường găng — ai chặn ai

```
Tùng: Entities theo 4 cụm ───┬──► Hiếu: Auth          (mở khoá sáng 23/09)
       (23–24/09, xem §3.1)  ├──► TV3:  Catalog       (mở khoá chiều 23/09)
                             ├──► TV6:  SaleBatch     (mở khoá sáng 24/09)
                             └──► Tùng: Ordering (T4)

Hiếu: Auth + JWT claims ─────────► mọi endpoint có phân quyền
       (HẠN: 29/09)

TV6: SaleBatch + Offer ──────────► Tùng: Ordering (T4)
       (HẠN: 29/09)

Tùng: Ordering + QR ─────────────► Hiếu: SignalR, BackgroundService
       (HẠN: 06/10)

TV4 ─┐
TV5 ─┴──► KHÔNG phụ thuộc ai — chạy trên MockApiClient, ghép API khi sẵn (§12.3)
```

**Hạn chặn người khác — trượt một ngày là nhiều người đứng im:**

| Hạn | Ai | Giao gì |
| --- | --- | --- |
| **23–24/09** | Tùng | Entity **theo 4 cụm**, xem lịch dưới |
| **Chủ Nhật 29/09** | Hiếu | Login trả JWT có `brand_id` / `store_id` |
| **Chủ Nhật 29/09** | TV6 | `SaleBatch` + `Offer` CRUD chạy được |
| **Thứ Hai 06/10** | Tùng | Đặt hàng + cấp/quét QR |

### 3.1. Giao entity theo cụm — không ai chờ quá nửa ngày

Đẩy một lần vào cuối Tuần 3 là sai: 4 người sẽ ngồi không mấy ngày đầu. Tùng đẩy lên
`dev` theo **4 cụm nhỏ**, xếp đúng thứ tự người đang chờ:

| Khi nào | Cụm entity | Mở khoá cho ai |
| --- | --- | --- |
| **Sáng T3 23/09** | `AppUser`, `AppRole`, `RefreshToken` | **Hiếu** — bắt đầu Auth ngay ngày đầu |
| **Chiều T3 23/09** | `Brand`, `Store`, `StoreStaff`, `Category`, `OfferTemplate` | **TV3** |
| **Sáng T4 24/09** | `SaleBatch`, `Offer` | **TV6** |
| **Chiều T4 24/09** | `Order`, `OrderItem`, `QrTicket`, `Payment`, `Review`, `Dispute`, `FavoriteStore`, `Notification` | Tùng tự dùng ở Tuần 4 |

Migration #1 chạy **một lần duy nhất sau cụm 4**, không chạy từng cụm — tránh 4 migration
cho cùng một lần dựng schema.

> **TV4 và TV5 không nằm trong đường găng.** Họ làm trên `MockApiClient` (§12.3), nên
> không phụ thuộc entity, không phụ thuộc endpoint, không bao giờ bị chặn.

---

## 4. Tuần 2 · 16–22/09 — Nền móng *(6 pd)*

| Người | Mã | Việc | pd |
| --- | --- | --- | --- |
| Tùng | `T2-TUNG-01` | Scaffold 6 project + `tests`, set reference đúng chiều, `dotnet build` sạch | 1.0 |
| Tùng | `T2-TUNG-02` | Khởi tạo Git: `main`/`dev`, bảo vệ nhánh, `.gitignore`, `appsettings.Development.json.example` | 0.5 |
| Tùng | `T2-TUNG-03` | **`Program.cs` hoàn chỉnh + 3 file `DependencyInjection.cs` rỗng** — sau mốc này không ai được sửa `Program.cs` (§12.1) | 0.5 |
| Hiếu | `T2-HIEU-01` | Đọc toàn bộ `docs/`, dựng khung `Features/Auth/` (interface rỗng) | 0.5 |
| Hiếu | `T2-HIEU-02` | Dựng cấu hình Swagger + Serilog cơ bản trong `Program.cs` | 0.5 |
| TV3 | `T2-TV3-01` | Chuẩn bị dữ liệu seed thật: 3 brand, 8 chi nhánh TP.HCM **có toạ độ thật**, 25 mẫu hàng | 0.5 |
| TV3 | `T2-TV3-02` | Dựng khung `Features/Catalog/` | 0.5 |
| TV4 | `T2-TV4-01` | Chọn template miễn phí, tạo **`Areas/Customer`** + layout riêng | 1.0 |
| TV5 | `T2-TV5-01` | Tạo **`Areas/Dashboard`** + layout riêng + menu theo vai trò | 1.0 |
| TV6 | `T2-TV6-01` | Tạo project gRPC + file `.proto` theo `03-API-CONTRACT.md` §11 | 0.5 |
| **Cả 6** | `T2-ALL-01` | Đọc `README.md`, `05-TEAM-RULES.md` §0, chạy `06-CODING-STANDARDS.md` §2 | — |

**Chốt 22/09:** ai cũng clone và `dotnet build` chạy được. Ai cũng đọc xong tài liệu.

---

## 5. Tuần 3 · 23–29/09 — Data layer + Auth *(15.75 pd)*

| Người | Mã | Việc | Liên quan | pd |
| --- | --- | --- | --- | --- |
| **Tùng** | `T3-TUNG-01` | **17 entity + enum + `ISoftDelete`/`IAuditable`** | `02` §3 | 1.5 |
| **Tùng** | `T3-TUNG-02` | `FreshRescueDbContext` + 17 file Configuration + Global Query Filter | `02` §3, `BR-22` | 1.0 |
| **Tùng** | `T3-TUNG-03` | **Migration #1** + `CHECK (RemainingQuantity >= 0)` | `02` §8 | 0.5 |
| **Tùng** | `T3-TUNG-04` | `IGenericRepository<T>` + `UnitOfWork` + `AddDataAccessLayer` | `06` §4.6 | 0.5 |
| **Tùng** | `T3-TUNG-05` | **`ErrorCodes.cs` đủ cả 28 mã** + `AppException` — chép thẳng từ contract, sau đó không ai thêm (§12.1) | `03` §1.4 | 0.25 |
| **Hiếu** | `T3-HIEU-01` | Identity + 4 role + `AppUser` | `FR-C01` | 0.5 |
| **Hiếu** | `T3-HIEU-02` | JWT + Refresh Token xoay vòng + `/auth/*` (8 endpoint) | `03` §2 | 1.5 |
| **Hiếu** | `T3-HIEU-03` | **Custom claim `brand_id`/`store_id`** + `RequireBrandOwner` / `RequireStoreStaff` | `BR-10`, `BR-11` | 1.0 |
| **TV3** | `T3-TV3-01` | CRUD `Category` + `Brand` + `Store` (kèm toạ độ) | `FR-B01`, `FR-B02`, `FR-A03` | 1.5 |
| **TV3** | `T3-TV3-02` | `DbSeeder` **pha 1 — dữ liệu tĩnh**: role, user, brand, store, category, template. Thời gian tính theo `UtcNow`, không `HasData` (§12.2) | `OQ-34`, `02` §8 | 1.0 |
| **TV4** | `T3-TV4-01` | **Hạ tầng WebClient dùng chung**: `IApiClient` + `HttpApiClient` + `MockApiClient` + xử lý token (§12.3). TV4 sở hữu, đóng băng sau Tuần 3 | | 1.5 |
| **TV4** | `T3-TV4-02` | Trang đăng nhập + storefront chạy trên `MockApiClient` | `FR-C03` | 1.0 |
| **TV5** | `T3-TV5-01` | Dashboard + điều hướng theo vai trò + quản lý chi nhánh — **chỉ trong `Areas/Dashboard`**, dùng `IApiClient` của TV4 | `FR-B02` | 1.5 |
| **TV5** | `T3-TV5-02` | Màn quản lý người dùng + duyệt thương hiệu (Admin) | `FR-A01`, `FR-A02` | 1.0 |
| **TV6** | `T3-TV6-01` | `SaleBatch` service + controller: tạo `Draft`, kích hoạt, **chặn chồng khung giờ** | `FR-S02`, `BR-15`, `OQ-11` | 1.5 |

**Chốt 29/09:** login trả JWT đủ claim · Swagger bấm Authorize được · seed data trong DB ·
CRUD Store chạy end-to-end.

---

## 6. Tuần 4 · 30/09–06/10 — Nghiệp vụ lõi *(17 pd — nặng nhất)*

| Người | Mã | Việc | Liên quan | pd |
| --- | --- | --- | --- | --- |
| **Tùng** | `T4-TUNG-01` | **`POST /orders`: transaction + optimistic locking + retry 3 lần + `409`** | `NFR-01`, `BR-01`…`BR-06` | 2.5 |
| **Tùng** | `T4-TUNG-02` | Sắp `Offer` theo `Id` chống deadlock + kiểm giới hạn mua cộng dồn | `02` §6.2, `BR-06` | 0.5 |
| **Tùng** | `T4-TUNG-03` | Thanh toán mock + cấp `QrTicket` (gọi gRPC) | `OQ-17`, `FR-C07` | 0.5 |
| **Tùng** | `T4-TUNG-04` | **`POST /staff/redemptions` idempotent** | `BR-08`, `BR-09`, `BR-10` | 1.0 |
| **Hiếu** | `T4-HIEU-01` | `SaleBatchSchedulerService` (`FR-X01`, `FR-X02`) | `02` §4.1 | 1.0 |
| **Hiếu** | `T4-HIEU-02` | `OrderExpiryService` — **hoàn tồn kho có chốt chặn** | `FR-X03`, `02` §6.4 | 1.0 |
| **Hiếu** | `T4-HIEU-03` | `NoShowService` + thứ tự chạy trước khi đóng đợt | `FR-X04`, `01` §6.5 | 1.0 |
| **TV3** | `T4-TV3-01` | CRUD `OfferTemplate` + validate biên độ giá hai tầng | `FR-B04`, `BR-07` | 1.0 |
| **TV3** | `T4-TV3-02` | **OData** `/odata/Stores` + `/odata/Offers` | `03` §9 | 1.5 |
| **TV4** | `T4-TV4-01` | Storefront nối API thật + lọc + chi tiết offer | `FR-C03` | 1.0 |
| **TV4** | `T4-TV4-02` | **Quick Checkout: đặt → thanh toán → hiện QR** | `01` §9.3 | 1.5 |
| **TV5** | `T4-TV5-01` | Màn Staff: tạo đợt bán 4 bước + quản lý offer | `FR-S02`, `FR-S03` | 1.5 |
| **TV5** | `T4-TV5-02` | Màn "đơn phải trả trong đợt" | `FR-S04` | 1.0 |
| **TV6** | `T4-TV6-01` | **gRPC QrService**: `IssueTicket` + `VerifyTicket`, ký HMAC, **không chạm DB** | `03` §11, `OQ-22` | 1.5 |
| **TV6** | `T4-TV6-02` | Đăng ký gRPC client ở API | | 0.5 |

**Chốt 06/10 — kịch bản bắt buộc chạy được:**
50 request đồng thời mua 10 suất → **đúng 10 đơn thành công, 40 đơn `409`**, tồn kho về 0
không âm. Quét QR lần hai trả `409`.

---

## 7. Tuần 5 · 07–13/10 — Rubric + phong toả tính năng *(15.25 pd)*

| Người | Mã | Việc | Liên quan | pd |
| --- | --- | --- | --- | --- |
| **Tùng** | `T5-TUNG-01` | Huỷ đơn: khách huỷ (`BR-13`) + cửa hàng huỷ (`BR-16`) + huỷ cả đợt (`BR-23`) | | 1.5 |
| **Tùng** | `T5-TUNG-02` | Exception Middleware + `ProblemDetails` + 28 mã lỗi | `03` §1.3–1.4 | 1.0 |
| **Tùng** | `T5-TUNG-03` | Review PR của cả nhóm, gỡ vướng | | 0.5 |
| **Hiếu** | `T5-HIEU-01` | **SignalR `OfferHub`** + bắn `OfferQuantityChanged` từ luồng đặt hàng | `FR-X06`, `03` §10 | 1.5 |
| **Hiếu** | `T5-HIEU-02` | `NotificationDispatcher` + bảng `Notification` (thông báo trong ứng dụng, không push) | `FR-X05`, `FR-C13` | 0.75 |
| **Hiếu** | `T5-HIEU-03` | Yêu thích cửa hàng | `FR-C12` | 0.5 |
| **TV3** | `T5-TV3-01` | Khiếu nại + hoàn tiền: khách gửi, Admin duyệt/từ chối | `FR-C15`, `FR-A04`, `BR-17` | 1.5 |
| **TV3** | `T5-TV3-02` | Content negotiation JSON + XML | Ch.03 | 0.5 |
| **TV3** | `T5-TV3-03` | Đánh giá + cập nhật `RatingAverage` bằng một câu SQL | `FR-C11`, `BR-12` | 0.5 |
| **TV4** | `T5-TV4-01` | Lịch sử đơn + trạng thái + huỷ đơn | `FR-C09`, `FR-C10` | 1.0 |
| **TV4** | `T5-TV4-02` | Gửi đánh giá + khiếu nại kèm ảnh (dùng endpoint upload chung) | `FR-C11`, `FR-C15`, `OQ-09` | 1.5 |
| **TV5** | `T5-TV5-01` | **Trang demo AJAX + SignalR JS** — realtime số suất còn lại (Ch.07) | `FR-X06` | 1.5 |
| **TV5** | `T5-TV5-02` | **Quét QR bằng webcam (HTML5-QRCode)** + ô nhập mã thủ công | `OQ-32`, `FR-S06` | 1.0 |
| **TV6** | `T5-TV6-01` | Endpoint upload ảnh dùng chung | `OQ-09` | 1.0 |
| **TV6** | `T5-TV6-02` | `DbSeeder` **pha 2 — dữ liệu giao dịch**: đợt bán + đơn hàng, sinh bằng **cách gọi service thật**, nên đồng thời là bài kiểm tra tích hợp (§12.2) | `OQ-34` | 0.5 |
| **TV6** | `T5-TV6-03` | **Xuất Postman collection** | | 0.5 |

**Chốt 13/10: PHONG TOẢ TÍNH NĂNG.** Từ đây chỉ commit `fix`, không `feat`.
Ai muốn thêm gì thì ghi vào mục "hướng phát triển" của báo cáo.

---

## 8. Tuần 6 · 14–20/10 — Kiểm thử, tài liệu, nộp *(13.5 pd)*

| Người | Mã | Việc | pd |
| --- | --- | --- | --- |
| **Tùng** | `T6-TUNG-01` | **Load test** k6/script C# + lấy số liệu so sánh optimistic vs pessimistic | 1.0 |
| **Tùng** | `T6-TUNG-02` | **Kịch bản demo 10 phút** + chạy thử đủ 2 lần với dữ liệu sạch | 1.0 |
| **Hiếu** | `T6-HIEU-01` | **Tổng hợp báo cáo** — ghép phần của 6 người, thống nhất giọng văn, mục lục | 2.0 |
| **Hiếu** | `T6-HIEU-02` | Sửa lỗi phát sinh | 1.0 |
| **TV3** | `T6-TV3-01` | Viết phần báo cáo của mình + sửa lỗi | 2.0 |
| **TV4** | `T6-TV4-01` | Viết phần báo cáo của mình + sửa lỗi | 2.0 |
| **TV5** | `T6-TV5-01` | Viết phần báo cáo của mình + slide | 2.0 |
| **TV6** | `T6-TV6-01` | **Unit test** cho đặt hàng + đối soát QR (dùng `FakeTimeProvider`) | 1.5 |
| **TV6** | `T6-TV6-02` | Viết phần báo cáo của mình | 1.0 |

### Phân công báo cáo

| Chương | Người |
| --- | --- |
| Giới thiệu đề tài, bối cảnh, mô hình kinh doanh | TV4 |
| Phân tích yêu cầu, use case, sơ đồ vai trò | TV5 |
| Thiết kế CSDL, ERD, máy trạng thái | Tùng |
| Kiến trúc hệ thống, N-Tier, Repository/UoW | Tùng |
| Bảo mật: JWT, Refresh Token, phân quyền đa cấp | Hiếu |
| **Xử lý đồng thời — optimistic vs pessimistic, có số liệu** | Tùng |
| Tiến trình nền, SignalR realtime | Hiếu |
| OData, content negotiation | TV3 |
| gRPC & hướng microservice | TV6 |
| Web Client & tích hợp AJAX | TV5 |
| Kiểm thử & kết quả load test | TV6 |
| Hạn chế và hướng phát triển | Hiếu tổng hợp |

**Chốt 20/10: NỘP.** API đóng băng.

---

## 9. Tuần 7 · 21–27/10 — Dự phòng

Không xếp việc. Dùng để bù cho tuần nào trượt. Nếu không trượt: tích hợp VNPay thật,
đánh bóng giao diện, hoặc nghỉ.

---

## 10. Nhịp kiểm soát hằng tuần

**Tối Chủ Nhật mỗi tuần**, 30 phút, cả 6 người:

1. Đối chiếu mốc chốt của tuần — **đạt hay không đạt, không có "gần xong"**
2. Ai trượt việc → nói ngay, không đợi tuần sau
3. Xem lại đường găng §3: tuần tới ai đang chặn ai
4. Quyết định có cần cắt phạm vi không (`00-PLAN.md` §1.2)

> Quy tắc một câu: **một việc chưa merge vào `dev` thì chưa xong.** Không tính "code
> xong rồi, chờ push".

---

## 11. Cắt gì nếu trượt

Khi cuối Tuần 4 chưa đạt chốt, cắt theo đúng thứ tự này. Người bị ảnh hưởng ghi phần đã
cắt vào mục "hướng phát triển" của báo cáo.

| Thứ tự | Cắt | Ai đỡ tải |
| --- | --- | --- |
| 1 | `FR-C12` yêu thích cửa hàng | Hiếu −0.5 |
| 2 | `FR-C15` + `FR-A04` khiếu nại | TV3 −1.5, TV4 −0.75 |
| 3 | `FR-S06` nhập mã đơn thủ công | TV5 −0.25 |
| 4 | `FR-C11` đánh giá *(cắt cuối — mất luôn `RatingAverage`)* | TV3 −0.5, TV4 −0.75 |

**Không bao giờ cắt:** đặt hàng + concurrency, đối soát QR, JWT phân quyền đa cấp,
OData, BackgroundService. Đó là phần ăn điểm rubric và phần bảo vệ trước giảng viên.

---

## 12. Chống dẫm chân lên nhau

Chia theo lát dọc giải quyết được xung đột ở **file nghiệp vụ**. Phần này giải quyết hai
loại còn lại: **file hạ tầng dùng chung** và **quyền ghi chéo lát**.

### 12.1. File dùng chung — mỗi file đúng một chủ

Những file này ai cũng có lý do chính đáng để sửa. Nếu không chỉ định chủ, chúng sẽ xung
đột mỗi tuần.

| File | Ai cũng muốn sửa | **Chủ duy nhất** | Người khác làm gì thay thế |
| --- | --- | --- | --- |
| `API/Program.cs` | Hiếu (JWT, Hub, jobs), TV3 (OData, middleware), TV6 (gRPC client), Tùng | **Tùng**, dựng đủ ở Tuần 2 | Chỉ sửa `DependencyInjection.cs` của tầng mình |
| `DataAccess/FreshRescueDbContext.cs` | TV3, TV6 muốn thêm `DbSet` | **Tùng**, khai đủ 17 `DbSet` ngay Tuần 3 | Configuration nằm file riêng, nạp bằng `ApplyConfigurationsFromAssembly` |
| `Business/Common/ErrorCodes.cs` | Cả 6 | **Tùng**, đủ 28 mã ở Tuần 3 | Thiếu mã ⇒ contract thiếu ⇒ sửa `03-API-CONTRACT.md` trước |
| `WebClient/` hạ tầng (`IApiClient`, layout gốc, `Program.cs`) | TV4, TV5 | **TV4**, xong Tuần 3 rồi đóng băng | TV5 chỉ làm trong `Areas/Dashboard` |
| `docs/03-API-CONTRACT.md` | Cả 6 | **Tùng** | Đề xuất qua nhóm chat, Tùng gộp một lần |
| `Directory.Packages.props` | Ai thêm package | **Tùng** | Nhắn xin thêm dòng |

> **Vì sao `Program.cs` phải dựng đủ từ Tuần 2 dù chưa có gì để chạy.** Gọi sẵn
> `AddDataAccessLayer()`, `AddBusinessLayer()`, `AddApiLayer()`, `MapHub<OfferHub>()`,
> `UseExceptionHandling()`… ngay cả khi các method đó còn rỗng. Mỗi người sau đó chỉ đổ
> code vào phần mở rộng của mình, không ai chạm vào `Program.cs` nữa.

### 12.2. Ranh giới quyền ghi — chống va chạm ngữ nghĩa

Loại va chạm nguy hiểm hơn xung đột merge: **hai người ghi cùng một dữ liệu từ hai file
khác nhau**. Git không báo gì, lỗi chỉ lộ ra lúc chạy.

| Dữ liệu | Ai được ghi | Ai **không** được |
| --- | --- | --- |
| `Offer.RemainingQuantity` | **Chỉ `OrderService`** (Tùng) | `OfferService` (TV6) — muốn đổi số lượng thì gọi `IOrderService.AdjustOfferStockAsync()` do Tùng viết |
| `Offer.TotalQuantity`, giá, thông tin chụp | **Chỉ `OfferService`** (TV6) | |
| `Order.Status` | **Chỉ `OrderService`** (Tùng) | BackgroundService của Hiếu — xem dưới |
| `SaleBatch.Status` | **Chỉ `SaleBatchService`** (TV6) | |
| `Store.RatingAverage` | **Chỉ `ReviewService`** (TV3), bằng một câu SQL | |

**BackgroundService không chứa logic nghiệp vụ.** Hiếu sở hữu vòng lặp và vòng đời;
chủ lát dọc sở hữu nội dung:

```csharp
// Hiếu viết. Không có if nghiệp vụ, không đụng DbContext.
using var scope = _scopeFactory.CreateScope();
await scope.ServiceProvider.GetRequiredService<IOrderService>()
           .ExpireOverdueOrdersAsync(ct);        // Tùng viết phần này
await scope.ServiceProvider.GetRequiredService<ISaleBatchService>()
           .CloseDueBatchesAsync(ct);            // TV6 viết phần này
```

Không có luật này, Hiếu sẽ phải viết lại logic hoàn tồn kho của Tùng — và hai bản sẽ
lệch nhau đúng ở chỗ khó nhất (`02-DOMAIN-MODEL.md` §6.4).

**Seeder tách hai pha** vì cùng lý do — TV3 không nên tự sinh đơn hàng:

| Pha | Ai | Khi nào | Nội dung |
| --- | --- | --- | --- |
| 1 — tĩnh | TV3 | Tuần 3 | Role, user, brand, store, category, template. Ghi thẳng qua `DbContext` |
| 2 — giao dịch | TV6 | Tuần 5 | Đợt bán + đơn hàng, sinh bằng **cách gọi service thật** |

Pha 2 gọi service thật nên vừa tạo dữ liệu demo, vừa là **bài kiểm tra tích hợp chạy mỗi
lần khởi động** — sai luồng đặt hàng là seed nổ ngay, không đợi tới lúc demo.

### 12.3. WebClient — tách đôi và không bao giờ bị chặn

Hai vấn đề: TV4 và TV5 chung một project, và cả hai phụ thuộc API chưa có.

**Tách không gian:**

```
WebClient/
├─ Areas/Customer/     ← TV4 sở hữu trọn
├─ Areas/Dashboard/    ← TV5 sở hữu trọn
├─ Services/           ← TV4 sở hữu, ĐÓNG BĂNG sau Tuần 3
│   ├─ IApiClient.cs
│   ├─ HttpApiClient.cs
│   └─ MockApiClient.cs
└─ Program.cs          ← TV4 sở hữu
```

**Cắt phụ thuộc bằng `IApiClient` hai bản cài đặt:**

```csharp
// appsettings.Development.json:  "UseMockApi": true
if (builder.Configuration.GetValue<bool>("UseMockApi"))
    builder.Services.AddScoped<IApiClient, MockApiClient>();
else
    builder.Services.AddScoped<IApiClient, HttpApiClient>();
```

`MockApiClient` trả thẳng các ví dụ JSON có sẵn trong `03-API-CONTRACT.md`. Nhờ vậy
TV4 và TV5 **không nằm trong đường găng**: họ dựng xong toàn bộ giao diện trong khi
backend còn chưa có endpoint, rồi chỉ việc lật cờ cấu hình.

Tốn thêm ~0.5 pd để viết `MockApiClient`, đổi lại **2 người không bao giờ phải ngồi chờ**
trong suốt 4 tuần. Và nó ép việc dùng đúng tên trường trong contract ngay từ đầu.

### 12.4. Đóng băng schema sau 29/09

Luật "chỉ Tùng tạo migration" đúng, nhưng tạo hàng đợi: TV3 cần thêm một cột ở Tuần 4
trong khi Tùng đang ngập trong concurrency thì TV3 đứng im.

**Từ 29/09, schema đóng băng.** Cả 17 bảng đã có đủ cột trong `02-DOMAIN-MODEL.md` §3 —
đó chính là lý do bỏ công vẽ ERD trước khi code. Sau mốc này, đổi schema là ngoại lệ, cần
lý do và được ghi vào nhật ký nhóm.

### 12.5. Kết quả

| Tuần | Số người chạy song song *(trước)* | *(sau)* |
| --- | --- | --- |
| T3 | 3 — Hiếu, TV3, TV6 chờ entity tới nửa tuần | **6 từ ngày đầu** |
| T4 | 5 — TV4 chờ endpoint đặt hàng | **6** |
| T5 | 6 | 6 |

Nói thẳng: với 6 người trên **một** backend, song song hoàn hảo là không thể. Mục tiêu
thực tế là đẩy va chạm ra khỏi **file** và biến nó thành va chạm ở **ranh giới ngữ nghĩa**
đã được đặt tên — nơi review PR bắt được, thay vì nơi Git im lặng.
