# FreshRescue — Kế hoạch triển khai PRN232

> Tài liệu gốc ý tưởng: `../PRN232.md`
> Cập nhật: 2026-09-29 — thêm yêu cầu báo cáo cuối kỳ (§1.3); 28/09 bỏ PRM393 (§0)
> Mục tiêu nộp PRN232: **cuối Tuần 6 — 20/10/2026**

---

## 0. Bối cảnh & phạm vi

|              | Nhóm PRN232                           |
| ------------ | ------------------------------------- |
| Thành viên   | **6 người** (Tùng, Hiếu + 4)          |
| Sản phẩm nộp | Web API + **MVC Web Client (C#)**     |
| Demo cuối kỳ | Chạy trên Web Client, dùng Swagger để minh hoạ API |

### Quyết định 28/09: không làm PRM393 trên dự án này

Bản trước của kế hoạch dùng lại backend cho đồ án Flutter (PRM393) và cho nhóm mobile chạy
song song. **Nhóm đã bỏ phương án đó.** Giao diện duy nhất để demo cho giảng viên là
**MVC Web Client** — vốn đã có trong kế hoạch từ đầu và phủ luôn Ch.07 (fetch/AJAX + SignalR JS).

Hệ quả:

- Không còn nhóm mobile chờ API → **bỏ** deploy bản dev ở Tuần 3 và gói bàn giao ở Tuần 6
  *(deploy được khôi phục ở Tuần 5–6 ngày 29/09, vì báo cáo có mục "Demo và deploy" — §1.3)*
- **Bỏ** những hạng mục chỉ tồn tại vì app di động: bảng `DeviceToken`, `/me/devices`
  (`FR-C14`), `?updatedSince=` (đồng bộ `sqflite`), mở vé QR khi mất mạng (`FR-C08`)
- **Giữ** token QR tự chứa + ký HMAC: lý do bây giờ là `OQ-22` (chỉ `QrService` giữ khoá,
  lý do chính đáng cho gRPC — Ch.09/Ch.10), không còn là offline
- **Giữ** khoá `Guid`, enum dạng chuỗi, `PagedResponse<T>` — vẫn là thực hành tốt, và
  WebClient cũng là một client cần hợp đồng ổn định

`docs/03-API-CONTRACT.md` vẫn là nguồn sự thật duy nhất — nay là hợp đồng giữa API và
Web Client.

---

## 1. Quyết định kỹ thuật đã chốt

| Hạng mục          | Quyết định                                       | Lý do                                                         |
| ----------------- | ------------------------------------------------ | ------------------------------------------------------------- |
| .NET              | **.NET 8 (LTS)**                                 | Đúng chuẩn slide Ch.00, khớp demo cũ, máy giảng viên chạy được |
| Kiến trúc         | **N-Tier 3 lớp** + Repository + Unit of Work     | Đúng đặc tả gốc, quen thuộc, ít boilerplate hơn Clean Arch     |
| CSDL              | SQL Server + EF Core 8 **Code-First Migration**  |                                                               |
| Auth              | ASP.NET Core Identity + JWT + Refresh Token      | Ch.06                                                         |
| Client demo       | **Swagger UI** + **ASP.NET Core MVC Web Client** | MVC dùng HttpClient + fetch/AJAX → phủ luôn Ch.07              |
| Tính năng ăn điểm | OData, SignalR, gRPC, BackgroundService          | Ch.04, Ch.09, Ch.10                                           |

### 1.1. Phạm vi đã cắt để vừa 5 tuần

Ước lượng ban đầu ~74 person-day; nguồn lực thực tế của 6 sinh viên trong 5 tuần
(~2–3h/ngày, đóng góp không đều) khoảng 65 pd. Các nhát cắt dưới đây đưa về ~54 pd,
**không mất điểm rubric nào**:

| Cắt                                                                | Tiết kiệm | Ghi chú                                                  |
| ------------------------------------------------------------------ | --------- | -------------------------------------------------------- |
| ~~Gộp `Product` vào `Offer`~~ — **đã thay bằng `OfferTemplate`**    | −3 → +2 pd| Xem `01-REQUIREMENTS.md` §3.1 và §1.2 dưới đây             |
| **1 dashboard dùng chung**, phân quyền bằng menu + template có sẵn | ~5 pd     | Không tự viết CSS                                         |
| **Không gửi FCM / push** — thông báo chỉ nằm trong bảng `Notification` | ~3 pd | Bảng `DeviceToken` đã bỏ hẳn ngày 28/09 (§0)          |
| **gRPC làm cực gọn** — 1 proto, 2 RPC, không DB                    | ~2 pd     | Vẫn đủ chấm Ch.09                                         |
| **VNPay mock**, không tích hợp thật                                | ~3 pd     | Chỉ làm thật nếu Tuần 7 còn dư                            |

> **Deploy cloud: khôi phục (29/09), làm ở Tuần 5–6.** Ngày 28/09 đã cắt vì không còn
> nhóm PRM393, nhưng khung báo cáo cuối kỳ có mục "Demo và deploy dự án" (§1.3). Deploy
> cả API, Web Client và QrService. Lưu ý gRPC cần HTTP/2 xuyên suốt — nhiều hosting giá
> rẻ không hỗ trợ, nên thử sớm ngay đầu Tuần 5.

### 1.2. Ngân sách cập nhật sau khi chốt yêu cầu (17/09)

Hai quyết định ở `01-REQUIREMENTS.md` §9 làm phạm vi tăng lên:

| Thay đổi | Ảnh hưởng |
| --- | --- |
| **Q-01** — tách `OfferTemplate` (Brand) khỏi `Offer` (trong đợt bán) | **+2 pd** — thêm một bộ CRUD cho Brand Manager. Hoàn lại phần đã cắt ở §1.1, nhưng đổi lấy thiết kế đúng hơn và vai trò Brand Manager có thực chất |
| **Q-06** — hủy đơn / hoàn tiền / khiếu nại, **bản gọn** | **+6.5 pd** — 3 nhánh hoàn tiền, cửa hàng hủy đơn, khiếu nại đơn giản. Đã hoãn voucher đền bù (tiết kiệm ~4 pd) và luồng khiếu nại đầy đủ (~2.5 pd) |

Lớp quyết định thứ hai (`01-REQUIREMENTS.md` §9.2) làm phạm vi đổi tiếp:

| Thay đổi | pd |
| --- | --- |
| `OQ-09` upload ảnh dùng chung (mặt hàng + đánh giá + khiếu nại) | +2 |
| `OQ-04` reset mật khẩu qua email | +1 |
| `OQ-32` quét QR bằng webcam trên web (HTML5-QRCode) | +1 |
| `OQ-16` 3 cột tiền + báo cáo phí sàn | +0.5 |
| `OQ-06` xoá mềm + global query filter | +0.5 |
| `OQ-11` chặn đợt bán chồng khung giờ | +0.5 |
| `OQ-31` **thu hẹp** phía khách trên web còn storefront + Quick Checkout | **−2** |
| `OQ-31` bổ sung màn hình hiện QR sau thanh toán (§9.3) | +0.5 |
| **Cộng** | **+4** |

| | pd |
| --- | --- |
| Ước lượng ban đầu | 54 |
| Sau lớp quyết định thứ nhất (Q-01…Q-10) | ~62.5 |
| Sau lớp quyết định thứ hai (OQ-01…OQ-35) | **~66.5** |
| Nguồn lực khả dụng tới cuối Tuần 6 | ~65 |
| **Dư** | **−1.5** *(âm nhẹ)* |

**Hệ quả:** ngân sách đã **vượt nhẹ**, không còn buffer trong 5 tuần.

**Đã cắt ngay 17/09** (không đợi tới cuối Tuần 4):

- `FR-B06` thống kê chi nhánh → rút còn **2 biểu đồ** (doanh thu + số suất đã cứu) — **−1 pd**
- `FR-A05` thống kê toàn sàn + `FR-A06` nhật ký đối soát → bỏ hẳn — **−2 pd**
  *(`NFR-05` vẫn được thoả bằng 2 cột `UsedAt` / `UsedByStaffUserId` trên `QrTicket`)*

| | pd |
| --- | --- |
| Sau hai nhát cắt | **~63.5** |
| Khả dụng | ~65 |
| **Dư** | **~1.5** |

**Bỏ PRM393 (28/09)** — các hạng mục chỉ phục vụ app di động:

| Cắt | pd |
| --- | --- |
| Deploy bản dev Tuần 3 (`T3-TV6-02`) | −0.5 |
| Gói bàn giao PRM393 (`T6-TUNG-03`) | −1 |
| `DeviceToken` + `/me/devices` (`FR-C14`) | −0.25 |
| `?updatedSince=` trên các endpoint danh mục | −0.25 |
| **Cộng** | **−2** |

| | pd |
| --- | --- |
| Sau khi bỏ PRM393 | **~61.5** |
| Khả dụng | ~65 |
| **Dư** | **~3.5** |

Biên an toàn vẫn mỏng, nhưng không còn âm sát.

### 1.3. Yêu cầu báo cáo cuối kỳ của giảng viên (29/09)

Khung báo cáo của cô (chi tiết và phân công từng mục ở `08-REPORT-REQUIREMENTS.md`) đòi
thêm mấy thứ chưa có trong phạm vi:

| Thêm | Mã | pd |
| --- | --- | --- |
| Import `OfferTemplate` từ Excel | `FR-B07`, `BR-26` | +1 |
| Export đơn + thống kê ra Excel (Brand + Admin) | `FR-B08`, `FR-A07`, `BR-27` | +1 |
| Nút Import/Export trên dashboard | | +0.5 |
| Seed **≥ 20 bản ghi mọi bảng** (cả hai pha seeder) | `NFR-09` | +1 |
| Khôi phục deploy (API + Web Client + QrService) | | +1 |
| ERD kiểu Chen + bảng chuyển ERD → schema theo mẫu của cô | | +0.5 |
| **Cộng** | | **+5** |

| | pd |
| --- | --- |
| Sau khi thêm yêu cầu báo cáo | **~66.5** |
| Khả dụng | ~65 |
| **Dư** | **−1.5** *(âm)* |

**Ngân sách âm trở lại.** Các yêu cầu trên là bắt buộc để không mất điểm báo cáo, nên
không cắt chúng. Nhóm cần chốt ở buổi họp Chủ Nhật: cắt thêm theo danh sách dưới đây
(bắt đầu từ `FR-C12`), hoặc chấp nhận dùng một phần Tuần 7.

> **Cần hỏi cô:** khung báo cáo yêu cầu "kế hoạch từ tuần 1 đến tuần 9", trong khi kế
> hoạch này nhắm nộp cuối Tuần 6 (20/10). Xác nhận lại hạn nộp và ngày thuyết trình thật.

Danh sách cắt tiếp theo giữ nguyên ở dưới, dùng khi cuối Tuần 4 chưa đạt chốt.

**Nếu tới cuối Tuần 4 mà chốt không đạt** (kịch bản 50 request đồng thời chưa chạy
đúng), cắt tiếp theo đúng thứ tự này — tất cả đều là mục *Nên có*, không mất điểm rubric:

1. `FR-C12` cửa hàng yêu thích *(kéo theo `FR-X05` mất đối tượng nhận thông báo)*
2. `FR-C15` + `FR-A04` khiếu nại → ghi vào "hướng phát triển" của báo cáo
3. `FR-S06` nhập mã đơn thủ công
4. `FR-C11` đánh giá *(cắt cuối cùng — mất luôn `Store.RatingAverage`)*

---

## 2. Cấu trúc Solution

```
D:\PRN232\Project\
├─ FreshRescue.sln
├─ docs/
│  ├─ 00-PLAN.md            ← file này
│  ├─ 01-REQUIREMENTS.md    ← phân tích yêu cầu, quy tắc nghiệp vụ, thuật ngữ
│  ├─ 02-DOMAIN-MODEL.md    ← ERD + state machine
│  ├─ 03-API-CONTRACT.md    ← nguồn sự thật + biên bản bàn giao
│  ├─ 04-OPEN-QUESTIONS.md  ← lý do đằng sau từng quyết định
│  ├─ 05-TEAM-RULES.md      ← luật hành vi & quy trình Git
│  ├─ 06-CODING-STANDARDS.md ← chuẩn code, khuôn mẫu, file cấu hình ép đồng nhất
│  ├─ 07-WORK-BREAKDOWN.md  ← phân công theo tuần cho 6 người
│  └─ 08-REPORT-REQUIREMENTS.md ← khung báo cáo cuối kỳ + ai viết mục nào
├─ .editorconfig · .gitattributes · global.json
├─ Directory.Build.props · Directory.Packages.props
├─ src/
│  ├─ FreshRescue.Entities/    (classlib) Entity, Enum, Constant — không phụ thuộc ai
│  ├─ FreshRescue.DataAccess/  (classlib) DbContext, Migrations, IRepository<T>, UnitOfWork, Seeder
│  ├─ FreshRescue.Business/    (classlib) DTO, IService/Service, AutoMapper, FluentValidation
│  ├─ FreshRescue.API/         (web)      Controller, OData, SignalR Hub, Middleware, BackgroundService
│  ├─ FreshRescue.QrService/   (grpc)     Cấp & xác thực QR token — Ch.09 + Ch.10
│  └─ FreshRescue.WebClient/   (mvc)      Dashboard + trang Customer
└─ tests/
   └─ FreshRescue.Tests/       (xunit)    Unit test + load test chống over-selling
```

**Chiều phụ thuộc (không được vi phạm):**

```
API → Business → DataAccess → Entities
```

WebClient chỉ gọi API qua HTTP, **không** reference thẳng DataAccess.

---

## 3. Lịch 5 tuần

Hôm nay là **Thứ Tư 16/09/2026, giữa Tuần 2**.

| Tuần  | Ngày            | Mục tiêu                       |
| ----- | --------------- | ------------------------------ |
| **2** | 16/09 – 22/09   | Thiết kế & nền móng            |
| **3** | 23/09 – 29/09   | Data layer + Auth              |
| **4** | 30/09 – 06/10   | Nghiệp vụ lõi (tuần nặng nhất) |
| **5** | 07/10 – 13/10   | Tính năng rubric + feature freeze |
| **6** | 14/10 – 20/10   | Test, tài liệu, **NỘP**        |
| **7** | 21/10 – 27/10   | **BUFFER — không xếp việc**    |

> **Tuần 7 là dự phòng, không phải tuần làm việc.** Nếu lên kế hoạch xong vào Tuần 7
> thì thực tế sẽ nộp Tuần 8. Luôn nhắm mốc 20/10.

### 3.1. Mốc ổn định API cho Web Client

Đức Anh và Hoàng Huy dựng giao diện trên `MockApiClient` (`07-WORK-BREAKDOWN.md` §12.3), rồi lật cờ
sang API thật khi từng phần sẵn sàng.

| Mốc                   | Tuần            | Web Client làm được gì                                   |
| --------------------- | --------------- | -------------------------------------------------------- |
| Contract trên giấy    | cuối T2 — 22/09 | Dựng màn hình trên `MockApiClient`                       |
| Auth + đọc dữ liệu    | cuối T3 — 29/09 | Đăng nhập, storefront, danh sách chi nhánh với seed thật |
| Luồng chính đủ        | cuối T4 — 06/10 | Đặt hàng, thanh toán mock, hiện QR, quét QR              |
| **Đóng băng API**     | cuối T5 — 13/10 | Hết đổi DTO — trùng mốc phong toả tính năng              |

---

### Tuần 2 (16–22/09) — Thiết kế & nền móng

Tuần này gần như không code feature. Đây là tuần quyết định 6 người có chạy song song
được hay không.

- [x] `01-REQUIREMENTS.md` — actor, thuật ngữ, FR/BR/NFR, các điểm cần chốt — *Tùng*
- [x] Chốt 45 câu hỏi mở (Q-01…Q-10 + OQ-01…OQ-35) — *Tùng*
- [x] `02-DOMAIN-MODEL.md` — ERD 18 bảng, chỉ mục, state machine, thiết kế concurrency — *Tùng*
- [ ] Cả nhóm đọc và duyệt `01-REQUIREMENTS.md` + `02-DOMAIN-MODEL.md` — *cả nhóm*
- [x] `03-API-CONTRACT.md` — ~60 endpoint + DTO + 28 mã lỗi + SignalR + proto — *Tùng*
- [ ] Scaffold solution + 6 project, set reference đúng chiều — *Tùng*
- [x] `05-TEAM-RULES.md` — luật hành vi & quy trình Git — *Tùng*
- [x] `06-CODING-STANDARDS.md` + `.editorconfig` / `.gitattributes` / `Directory.*.props` / `global.json` — *Tùng*
- [ ] Git repo, nhánh `main`/`dev`, `.gitignore`, bảo vệ nhánh — *Tùng*
- [ ] Cả nhóm đọc `05-TEAM-RULES.md` §0 + chạy `06-CODING-STANDARDS.md` §2 (cài đặt máy) — *cả nhóm*
- [ ] Cả 6 người cài môi trường, clone và `dotnet build` chạy được

**Chốt 22/09:** ERD + contract được cả nhóm duyệt. Ai cũng build được solution rỗng.

> Nếu tới 22/09 vẫn đang cãi nhau về schema thì **dừng mọi việc khác để giải quyết** —
> 4 người còn lại sẽ ngồi chơi, và cả kế hoạch trượt theo.

---

### Tuần 3 (23–29/09) — Data layer + Auth

- [ ] Entities + `FreshRescueDbContext` + Fluent config + **Migration #1** — *Tùng*
- [ ] `IGenericRepository<T>` + `UnitOfWork` — *Tùng*
- [ ] Identity + JWT + Refresh Token + **custom claim `brand_id` / `store_id`**
      + policy handler `RequireBrandOwner` / `RequireStoreStaff` — *Tùng*
- [ ] `DbSeeder`: 4 role, admin, 2 brand, 4 store, ~20 offer mẫu — *Đình Đức*
- [ ] CRUD Brand / Store / Category — *Đình Đức*
- [ ] SaleSession + Offer (entity → service → controller) — *Hiếu*
- [ ] Khung Web Client: layout, template admin, `HttpClient` service, trang login — *Đức Anh + Hoàng Huy*
- [ ] Project gRPC + file `.proto`, cấu hình Swagger — *Quang Huy*

**Chốt 29/09:** login trả JWT có đủ claim, Swagger bấm Authorize dùng được, seed data
nằm trong DB, CRUD Store chạy end-to-end.

---

### Tuần 4 (30/09–06/10) — Nghiệp vụ lõi

Tuần nặng nhất. Đừng xếp thêm gì vào đây.

- [ ] **Đặt hàng + transaction + optimistic locking + retry + `409`** — *Tùng*
- [ ] Cấp QR ticket khi thanh toán thành công — *Tùng*
- [ ] gRPC QrService + **redeem idempotent**; payment mock — *Quang Huy (phối hợp Tùng)*
- [ ] Lifecycle đợt bán + **4 BackgroundService** (scheduler, expiry, no-show, nhắc giờ) — *Hiếu*
- [ ] **OData** endpoints cho `Stores` / `Offers` — *Đình Đức*
- [ ] Web Client nối API thật: browse, đặt hàng, dashboard CRUD — *Đức Anh + Hoàng Huy*
- [ ] **Hoàn tất checklist §4.4** — *Tùng + Hiếu duyệt*

**Chốt 06/10 — kịch bản demo:** 50 request đồng thời mua 10 túi → đúng 10 đơn thành
công, 40 đơn `409`, tồn kho về 0 không âm. Quét QR lần 2 trả `409`.

---

### Tuần 5 (07–13/10) — Tính năng rubric + Feature freeze

- [ ] **SignalR** `OfferHub` — realtime số lượng còn lại + sự kiện mở đợt bán — *Hiếu*
- [ ] SignalR JS client + trang demo fetch/AJAX (Ch.07) — *Hoàng Huy*
- [ ] Exception Middleware tập trung, content negotiation (JSON + XML), chuẩn status code — *Đình Đức*
- [ ] Swagger XML comment + Postman collection — *Quang Huy*
- [ ] **Deploy** API + Web Client + QrService — *Quang Huy*
- [ ] **Import `OfferTemplate` từ Excel** — *Đình Đức*; **Export đơn ra Excel** — *Hiếu*; nút trên dashboard — *Hoàng Huy*
- [ ] Seed đủ **≥ 20 bản ghi mọi bảng** — *Đình Đức* (pha 1), *Quang Huy* (pha 2)
- [ ] Hoàn thiện trang customer + hiển thị QR — *Đức Anh*

**Chốt 13/10: PHONG TOẢ TÍNH NĂNG.** Từ đây chỉ sửa lỗi, không thêm chức năng mới.
Ai muốn thêm gì thì ghi vào mục "hướng phát triển" của báo cáo.

---

### Tuần 6 (14–20/10) — Test, tài liệu, nộp

- [ ] Unit test cho đặt hàng + redeem QR — *Quang Huy*
- [ ] Load test lấy số liệu chứng minh không over-sell — *Quang Huy*
- [ ] Báo cáo **bằng tiếng Anh** theo khung của cô: mỗi người viết phần mình làm, *Hiếu* tổng hợp (`08-REPORT-REQUIREMENTS.md`)
- [ ] ERD kiểu Chen + bảng chuyển ERD → schema + ảnh thiết kế và dữ liệu của từng bảng — *Tùng*
- [ ] Slide + kịch bản demo 10 phút trên Web Client, **chạy thử ít nhất 2 lần** — *Tùng*

**Chốt 20/10: NỘP.** API đóng băng — từ đây đổi gì phải version (`/api/v2`).

---

### Tuần 7 (21–27/10) — Buffer

Không xếp việc. Dùng để bù cho tuần nào bị trượt. Nếu không trượt thì dành cho:
tích hợp VNPay thật, đánh bóng UI, hoặc nghỉ.

---

## 4. Ba điểm kỹ thuật "đinh" — phải làm cho tới

### 4.1. Chống over-selling (flash sale túi mù)

Đây là thứ phân biệt đồ án này với một bài CRUD tầm thường.

**Optimistic locking** (mặc định):

- `Offer` có cột `RowVersion` (`[Timestamp]`)
- Đặt hàng → đọc Offer → kiểm `RemainingQuantity` → trừ → `SaveChanges()`
- Bắt `DbUpdateConcurrencyException` → retry tối đa 3 lần → hết thì trả `409 Conflict`

**Pessimistic locking** (để so sánh trong báo cáo):

- `BEGIN TRAN` + `SELECT ... WITH (UPDLOCK, ROWLOCK)` qua `FromSqlRaw`

→ Báo cáo nên trình bày **cả hai**, kèm số liệu load test. Điểm cộng rất lớn.

### 4.2. Phân quyền đa cấp bằng Custom Claims

Không dùng `[Authorize(Roles="...")]` suông. JWT phải chứa `brand_id` / `store_id`, rồi:

- Brand Manager chỉ sửa được Store thuộc `brand_id` của mình
- Store Staff chỉ redeem được đơn thuộc `store_id` của mình

→ Viết `AuthorizationHandler` riêng, **không** nhét logic này vào controller.

### 4.3. QR Redemption idempotent

- Token QR ký bằng HMAC, có `exp` = `PickupEndAt`
- Redeem phải: kiểm chữ ký → kiểm đơn thuộc đúng store của staff → kiểm trạng thái →
  `UPDATE ... WHERE Status = 'Paid'` và check số dòng ảnh hưởng
- Quét lần 2 phải trả `409 Conflict`, không được trừ hàng lần nữa

### 4.4. Hạng mục thiết kế cho mobile — giữ hay bỏ (sau 28/09)

Bản trước của mục này là checklist "chuẩn bị sẵn cho Flutter". Không còn app di động, từng
mục được xét lại:

| Hạng mục | Quyết định | Lý do |
| --- | --- | --- |
| Token QR **tự chứa** + ký HMAC (`orderId`, `storeId`, `exp`) | **Giữ** | `OQ-22`: chỉ `QrService` giữ khoá — lý do chính đáng cho gRPC (Ch.09/Ch.10). Không còn yêu cầu mở vé offline |
| `Store.Latitude` / `Longitude` + `/stores/nearby` | **Giữ** | Storefront web vẫn lọc theo khoảng cách (`FR-C02`); seed đã có toạ độ thật |
| `CreatedAt` / `UpdatedAt` trên entity chính | **Giữ** | `IAuditable`, phục vụ truy vết |
| `?updatedSince=` trên endpoint danh mục | **Bỏ** | Chỉ phục vụ đồng bộ `sqflite` |
| `PagedResponse<T>` cho mọi danh sách | **Giữ** | `NFR-06` |
| Bảng `DeviceToken` + `/me/devices` | **Bỏ** | Chỉ phục vụ push FCM của app di động (`FR-C14` đã cắt) |
| Lưu URL ảnh, không lưu base64 | **Giữ** | Vẫn đúng với web |

---

## 5. Phân công 6 người

Cả 6 người đều làm backend/web.

> **Phân công chi tiết theo tuần, có mã việc và ước lượng pd: `07-WORK-BREAKDOWN.md`.**
> Bảng dưới đây chỉ là bản tóm tắt. Nơi hai tài liệu khác nhau thì `07` thắng — xem §1 của nó
> về việc đổi chủ sở hữu giữa Hiếu và Quang Huy.

| Người         | Phụ trách                                                                                                       |
| ------------- | --------------------------------------------------------------------------------------------------------------- |
| **Tùng**      | Lead. Scaffold, Entities/DbContext, **giữ quyền tạo Migration**, Repo/UoW, Auth/JWT/Claims, **đặt hàng + concurrency**, cấp QR, kịch bản demo |
| **Hiếu**      | SaleSession + Offer, 4 BackgroundService, SignalR Hub, tổng hợp báo cáo                                          |
| **Đình Đức**  | CRUD Brand/Store/Category, **OData**, DbSeeder, Exception Middleware + content negotiation                        |
| **Đức Anh**   | Web Client — trang public + customer (browse, đặt hàng, xem QR)                                                   |
| **Hoàng Huy** | Web Client — dashboard chung (Admin + Brand Manager), trang demo AJAX + SignalR JS                                |
| **Quang Huy** | gRPC QrService, redeem idempotent, payment mock, Swagger/Postman, unit test + load test                           |

Tùng + Hiếu review PR để giữ contract nhất quán.

### 5.1. Hai quy tắc bắt buộc khi nhóm đông

**Chỉ Tùng được tạo migration.** Đây là tai nạn kinh điển của nhóm 6 người: ai cũng
`Add-Migration`, snapshot xung đột, và merge conflict trong file migration gần như
không sửa được. Ai cần đổi schema thì báo Tùng.

**Chia theo lát dọc, không chia theo tầng.** Mỗi người sở hữu trọn bộ
controller + service + DTO của phần mình. Chia theo tầng (một người làm hết service,
một người làm hết controller) sẽ khiến ai cũng sửa cùng file và merge liên tục.

---

## 6. Rủi ro & cách xử lý

| Rủi ro                                                                | Mức        | Xử lý                                                                                                              |
| --------------------------------------------------------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------ |
| **Tuần 2 không chốt được thiết kế**                                   | **Cao**    | Điều kiện sống còn để 6 người chạy song song. Trượt mốc 22/09 là trượt cả kế hoạch                                  |
| Web Client (~13 pd) và báo cáo (~5 pd) bị dồn tới tuần cuối           | **Cao**    | Đức Anh + Hoàng Huy bắt đầu từ Tuần 3 với API giả. Báo cáo viết dần từng tuần, không để tới Tuần 6                            |
| Ôm đồm quá nhiều (gRPC + OData + SignalR)                             | Trung bình | Nếu hụt, cắt từ cuối lên theo thứ tự ngược: gRPC → SignalR → BackgroundService → OData                              |
| Concurrency khó debug                                                 | Trung bình | Làm ở Tuần 4, viết test tự động ngay, đừng để sát deadline                                                          |
| Đóng góp lệch — 2 người gánh cả nhóm                                  | Trung bình | Mỗi lát dọc có một chủ sở hữu rõ ràng và mốc chốt cuối tuần. Phát hiện chậm ở mốc tuần, không phải ở tuần cuối      |

---

## 7. Việc tiếp theo ngay (Tuần 2)

1. Viết `02-DOMAIN-MODEL.md` — ERD + state machine đơn hàng
2. Viết `03-API-CONTRACT.md` — danh sách endpoint
3. Scaffold solution + 6 project + git
4. Cả nhóm duyệt thiết kế trước 22/09
