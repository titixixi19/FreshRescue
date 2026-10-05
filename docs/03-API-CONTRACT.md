# FreshRescue — API Contract

> Yêu cầu: `01-REQUIREMENTS.md` · Mô hình dữ liệu: `02-DOMAIN-MODEL.md`
> Cập nhật: 2026-09-29 — thêm Import/Export Excel (§7.1, §8) · **Phiên bản contract: v1 (chưa đóng băng)**
>
> ## Đây là nguồn sự thật duy nhất
>
> Đổi endpoint hoặc DTO thì **sửa file này trước, rồi mới code**. Đây là hợp đồng giữa
> API và Web Client — `MockApiClient` của Web Client dựng từ chính các ví dụ JSON ở đây.
>
> **Đóng băng cuối Tuần 5 (13/10).** Sau mốc đó, thay đổi phá vỡ tương thích phải lên
> `/api/v2`.

---

## 1. Quy ước chung

| Hạng mục | Quy ước |
| --- | --- |
| Base URL | `{host}/api/v1` |
| OData | `{host}/odata` — ngoài versioning, xem §9 |
| SignalR | `{host}/hubs/offers` — xem §10 |
| Định dạng | JSON (mặc định). Hỗ trợ `Accept: application/xml` — Ch.03 |
| Xác thực | `Authorization: Bearer <access_token>` |
| Thời gian | **ISO-8601 UTC, luôn có hậu tố `Z`**: `2026-09-17T13:30:00Z` (`OQ-33`) |
| Tiền | **Số nguyên**, đơn vị VND. `70000` nghĩa là 70.000₫. Không có phần lẻ |
| Id | `Guid` dạng chuỗi |
| Enum | **Chuỗi PascalCase** trong JSON (`"PendingPayment"`), không phải số |
| Toạ độ | `number`, 6 chữ số thập phân |

> **Enum trả về dạng chuỗi, không phải số.** Lưu trong CSDL là `tinyint` cho gọn, nhưng
> API luôn trả chuỗi — nếu trả số, Web Client (C# lẫn JS) sẽ phải tự map và mỗi lần chèn
> thêm một giá trị enum là vỡ giao diện. Cấu hình `JsonStringEnumConverter` ngay từ đầu.

### 1.1. Phân trang

Mọi endpoint danh sách nhận `?page=1&pageSize=20` (mặc định 20, tối đa 100) và trả:

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137,
  "totalPages": 7
}
```

### 1.2. ~~Đồng bộ cache cho mobile~~ — đã bỏ 28/09

`?updatedSince=` chỉ phục vụ `sqflite` của app di động. Không còn app di động nên **không**
hiện thực. Giữ số mục để các tham chiếu tới §1.3, §1.4 không bị lệch.

### 1.3. Khuôn lỗi — RFC 7807 ProblemDetails

Mọi lỗi trả cùng một khuôn:

```json
{
  "type": "https://freshrescue.dev/errors/offer-sold-out",
  "title": "Mặt hàng đã hết suất",
  "status": 409,
  "detail": "Túi mù bánh ngọt chỉ còn 1 suất, bạn đặt 2.",
  "instance": "/api/v1/orders",
  "errorCode": "OFFER_SOLD_OUT",
  "traceId": "00-a1b2...-01"
}
```

Riêng lỗi kiểm tra dữ liệu (`400`) có thêm `errors`:

```json
{
  "status": 400,
  "errorCode": "VALIDATION_FAILED",
  "errors": {
    "items[0].quantity": ["Số lượng phải lớn hơn 0"]
  }
}
```

> **Client bắt theo `errorCode`, không bắt theo `detail`.** `detail` là câu tiếng Việt
> để hiển thị cho người dùng và có thể đổi bất cứ lúc nào; `errorCode` là hợp đồng.

### 1.4. Mã lỗi

Mỗi mã gắn với một quy tắc nghiệp vụ — tra ngược về `01-REQUIREMENTS.md`.

| `errorCode` | HTTP | Quy tắc | Khi nào |
| --- | --- | --- | --- |
| `VALIDATION_FAILED` | 400 | — | Dữ liệu vào sai định dạng |
| `UNAUTHENTICATED` | 401 | — | Thiếu hoặc sai token |
| `TOKEN_EXPIRED` | 401 | — | Access token hết hạn → gọi `/auth/refresh` |
| `FORBIDDEN_SCOPE` | 403 | `BR-10` `BR-11` | Đúng role nhưng sai phạm vi chi nhánh / thương hiệu |
| `NOT_FOUND` | 404 | — | |
| `BRAND_NOT_APPROVED` | 403 | `BR-14` | Thương hiệu chưa được duyệt mà đã mở đợt bán |
| `BATCH_NOT_ACTIVE` | 409 | `BR-02` | Đợt bán chưa kích hoạt hoặc đã đóng |
| `SALE_WINDOW_CLOSED` | 409 | `BR-02` | Ngoài khoảng `[SaleStartTime, SaleEndTime]` |
| `OFFER_SOLD_OUT` | 409 | `BR-01` | Không đủ tồn kho |
| `OFFER_CROSS_BATCH` | 400 | `BR-03` | Các dòng không cùng một đợt bán |
| `PURCHASE_LIMIT_EXCEEDED` | 409 | `BR-06` | Vượt 3 suất/offer/đợt (tính cả đơn đã nhận) |
| `PRICE_OUT_OF_BAND` | 400 | `BR-07` | Giá ngoài biên độ hai tầng |
| `PAYMENT_DEADLINE_PASSED` | 409 | `BR-05` | Thanh toán sau hạn giữ chỗ |
| `ORDER_NOT_PAYABLE` | 409 | — | Đơn không ở trạng thái `PendingPayment` |
| `CANCEL_WINDOW_PASSED` | 409 | `BR-13` | Quá hạn huỷ |
| `REDEEM_WINDOW_CLOSED` | 400 | `BR-08` | Ngoài `[PickupStartTime − 5', PickupEndTime]` |
| `ORDER_ALREADY_REDEEMED` | 409 | `BR-09` | Quét lần thứ hai |
| `QR_INVALID_SIGNATURE` | 400 | — | Chữ ký sai hoặc token bị sửa |
| `QR_EXPIRED` | 400 | — | Token quá hạn |
| `BATCH_TIME_INVALID` | 400 | `BR-15` | Bốn mốc thời gian không hợp lệ |
| `BATCH_OVERLAP` | 409 | `OQ-11` | Trùng khung giờ bán với đợt khác cùng chi nhánh |
| `BATCH_LOCKED` | 409 | `OQ-14` | Sửa thứ bị khoá sau đơn `Paid` đầu tiên |
| `BATCH_HAS_REDEEMED_ORDERS` | 409 | `BR-23` | Huỷ đợt đã có đơn đã trao hàng |
| `OFFER_HAS_ORDERS` | 409 | `BR-24` | Xoá offer đã có đơn |
| `REVIEW_ALREADY_EXISTS` | 409 | `BR-12` | |
| `DISPUTE_WINDOW_PASSED` | 409 | `BR-17` | Quá 2 giờ kể từ `RedeemedAt` |
| `DISPUTE_ALREADY_EXISTS` | 409 | `BR-17` | |
| `CONCURRENCY_CONFLICT` | 409 | `NFR-01` | Hết 3 lần thử lại khi tranh chấp tồn kho |

### 1.5. Claims trong JWT

Web Client (Đức Anh, Hoàng Huy) đọc kỹ phần này.

```json
{
  "sub":       "3f2a...",
  "email":     "an@example.com",
  "name":      "Nguyễn Văn An",
  "role":      ["Customer", "StoreStaff"],
  "brand_id":  "8c1e...",
  "store_id":  "b77d...",
  "exp":       1789000000
}
```

| Claim | Ghi chú |
| --- | --- |
| `role` | **Mảng** — một user có nhiều vai trò (`OQ-03`). Luôn xử lý như mảng kể cả khi chỉ có một phần tử |
| `brand_id` | Chỉ có khi mang vai trò `BrandManager` |
| `store_id` | Chỉ có khi mang vai trò `StoreStaff`. **Bỏ qua claim này khi người dùng mua hàng** |

- Access token sống **15 phút** (`02-DOMAIN-MODEL.md` §3.2 giải thích vì sao ngắn)
- Refresh token sống **7 ngày**, **xoay vòng** mỗi lần dùng

---

## 2. Xác thực — `/auth`

| Method | Đường dẫn | Quyền | Mô tả |
| --- | --- | --- | --- |
| POST | `/auth/register` | Công khai | Đăng ký khách hàng |
| POST | `/auth/register-brand` | Công khai | Đăng ký thương hiệu → `PendingApproval` (`OQ-01`) |
| POST | `/auth/login` | Công khai | |
| POST | `/auth/refresh` | Công khai | Đổi refresh token lấy cặp token mới |
| POST | `/auth/logout` | Đăng nhập | Thu hồi refresh token |
| POST | `/auth/forgot-password` | Công khai | Gửi email chứa token đặt lại |
| POST | `/auth/reset-password` | Công khai | |
| GET | `/auth/me` | Đăng nhập | Hồ sơ + vai trò + phạm vi |

**`POST /auth/login`**

```json
// request
{ "email": "an@example.com", "password": "..." }

// 200
{
  "accessToken": "eyJ...",
  "refreshToken": "9f3c...",
  "expiresAt": "2026-09-17T12:15:00Z",
  "user": {
    "id": "3f2a...",
    "fullName": "Nguyễn Văn An",
    "email": "an@example.com",
    "roles": ["Customer"],
    "brandId": null,
    "storeId": null
  }
}
```

**`POST /auth/register-brand`** — tạo cùng lúc tài khoản Brand Manager và hồ sơ thương
hiệu ở trạng thái `PendingApproval` (`OQ-02`). Đăng nhập được ngay, nhưng mọi thao tác
nghiệp vụ trả `BRAND_NOT_APPROVED` cho tới khi Admin duyệt.

---

## 3. Công khai — danh mục & tìm kiếm

| Method | Đường dẫn | Quyền | Mô tả |
| --- | --- | --- | --- |
| GET | `/categories` | Công khai | Danh mục |
| GET | `/stores/nearby` | Công khai | Cửa hàng gần một toạ độ (`FR-C02`) |
| GET | `/stores/{id}` | Công khai | Chi tiết chi nhánh + đợt bán đang mở |
| GET | `/stores/{id}/reviews` | Công khai | Đánh giá, phân trang |
| GET | `/offers/active` | Công khai | Offer đang bán, có bộ lọc (`FR-C03`) |
| GET | `/offers/{id}` | Công khai | Chi tiết một offer |

**`GET /stores/nearby`**

| Tham số | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `lat`, `lng` | ✅ | Vị trí người dùng |
| `radiusKm` | | Mặc định 5, tối đa 50 |
| `categoryId` | | |
| `hasActiveBatch` | | `true` = chỉ chi nhánh đang có đợt bán mở |
| `page`, `pageSize` | | |

```json
// 200 — PagedResponse<StoreNearbyDto>
{
  "items": [{
    "id": "b77d...",
    "brandId": "8c1e...",
    "brandName": "Tous les Jours",
    "name": "Chi nhánh Nguyễn Huệ",
    "addressLine": "12 Nguyễn Huệ, Bến Nghé",
    "district": "Quận 1",
    "city": "TP. Hồ Chí Minh",
    "latitude": 10.774100,
    "longitude": 106.703500,
    "distanceKm": 1.24,
    "openTime": "07:00:00",
    "closeTime": "22:00:00",
    "ratingAverage": 4.6,
    "ratingCount": 128,
    "activeBatchCount": 1,
    "minPrice": 29000,
    "updatedAt": "2026-09-17T09:12:00Z"
  }],
  "page": 1, "pageSize": 20, "totalCount": 8, "totalPages": 1
}
```

> `distanceKm` do server tính bằng Haversine sau khi lọc hộp bao
> (`02-DOMAIN-MODEL.md` §5.1). Client **không** phải tự tính.

**`GET /offers/active`** — lọc: `storeId`, `categoryId`, `offerType`, `minPrice`,
`maxPrice`, `lat`+`lng`+`radiusKm`, `inStockOnly`.

```json
{
  "id": "e91b...",
  "saleBatchId": "77aa...",
  "storeId": "b77d...",
  "storeName": "Chi nhánh Nguyễn Huệ",
  "distanceKm": 1.24,
  "name": "Túi mù bánh ngọt",
  "description": "3–5 món bánh ngọt trong ngày",
  "imageUrl": "/uploads/offers/abc.jpg",
  "categoryName": "Bánh ngọt",
  "offerType": "SurpriseBag",
  "originalPrice": 120000,
  "actualPrice": 35000,
  "discountPercent": 70.8,
  "remainingQuantity": 4,
  "maxPerCustomer": 3,
  "saleStartTime":   "2026-09-17T12:30:00Z",
  "saleEndTime":     "2026-09-17T14:00:00Z",
  "pickupStartTime": "2026-09-17T13:30:00Z",
  "pickupEndTime":   "2026-09-17T14:30:00Z"
}
```

---

## 4. Khách hàng — đặt hàng

| Method | Đường dẫn | Quyền | Mô tả |
| --- | --- | --- | --- |
| POST | `/orders` | Customer | **Đặt hàng** — giữ chỗ (`FR-C05`) |
| POST | `/orders/{id}/payment` | Customer | Thanh toán giả lập (`OQ-17`) |
| POST | `/orders/{id}/cancel` | Customer | Huỷ (`BR-13`) |
| GET | `/orders/{id}/qr` | Customer | Lấy vé QR (`FR-C07`) |
| GET | `/me/orders` | Customer | Lịch sử, lọc `status` |
| GET | `/me/orders/{id}` | Customer | Chi tiết |

### 4.1. `POST /orders` — endpoint quan trọng nhất

```json
// request
{
  "saleBatchId": "77aa...",
  "items": [
    { "offerId": "e91b...", "quantity": 2 },
    { "offerId": "a03f...", "quantity": 1 }
  ]
}
```

```json
// 201 Created
{
  "id": "0c4d...",
  "orderCode": "FR-8A3K9MQ2",
  "status": "PendingPayment",
  "storeId": "b77d...",
  "storeName": "Chi nhánh Nguyễn Huệ",
  "storeAddress": "12 Nguyễn Huệ, Bến Nghé, Quận 1",
  "saleBatchId": "77aa...",
  "pickupStartTime": "2026-09-17T13:30:00Z",
  "pickupEndTime":   "2026-09-17T14:30:00Z",
  "paymentDeadline": "2026-09-17T12:45:00Z",
  "totalAmount": 105000,
  "items": [
    { "offerId": "e91b...", "offerName": "Túi mù bánh ngọt",
      "quantity": 2, "unitPrice": 35000, "lineTotal": 70000 }
  ],
  "createdAt": "2026-09-17T12:35:00Z"
}
```

> **`platformFee` và `storePayoutAmount` KHÔNG trả cho Customer.** Khách không cần biết
> sàn ăn bao nhiêu phần trăm. Hai trường này chỉ xuất hiện trong response của
> Brand Manager và Admin.

**Các lỗi có thể gặp** — client nên xử lý riêng từng mã:

| `errorCode` | Ý nghĩa với người dùng |
| --- | --- |
| `SALE_WINDOW_CLOSED` | "Đợt bán đã kết thúc" |
| `OFFER_SOLD_OUT` | "Có người vừa mua mất" — nên tải lại danh sách |
| `PURCHASE_LIMIT_EXCEEDED` | "Mỗi người chỉ mua tối đa 3 suất" |
| `OFFER_CROSS_BATCH` | Lỗi lập trình phía client, không phải lỗi người dùng |
| `CONCURRENCY_CONFLICT` | "Hệ thống đang bận" — cho phép thử lại |
| `VALIDATION_FAILED` | `items` sai định dạng — client nên chặn trước khi gửi: từ **1 đến 20** dòng, mỗi `offerId` **chỉ một dòng** (không gộp), `quantity` từ **1 đến 10** |

### 4.2. `POST /orders/{id}/payment`

```json
// request — cổng thanh toán giả (OQ-17)
{ "outcome": "Success" }     // hoặc "Failure"
```

```json
// 200 khi Success
{
  "order": { "...": "OrderDto", "status": "Paid", "paidAt": "..." },
  "qrTicket": {
    "token": "eyJvcmRlcklkIjoi...Qk1n",
    "expiresAt": "2026-09-17T14:30:00Z"
  }
}
```

`Failure` trả `200` với đơn vẫn `PendingPayment` — **không** huỷ ngay, tồn kho vẫn được
giữ cho tới hết hạn (`OQ-17`).

### 4.3. `GET /orders/{id}/qr`

```json
{
  "orderCode": "FR-8A3K9MQ2",
  "token": "eyJvcmRlcklkIjoi...Qk1n",
  "expiresAt": "2026-09-17T14:30:00Z",
  "pickupStartTime": "2026-09-17T13:30:00Z",
  "storeName": "Chi nhánh Nguyễn Huệ"
}
```

> **Ghi cho Web Client.** Vẽ `token` thành ảnh QR ngay trên trang, kèm `orderCode` in
> to để nhân viên nhập tay khi webcam trục trặc
> (`FR-S06`, `OQ-32`). `token` tự chứa dữ liệu và đã ký (`NFR-04`) — đừng cắt ngắn hay
> định dạng lại chuỗi.

---

## 5. Khách hàng — sau khi nhận hàng & cá nhân

| Method | Đường dẫn | Quyền | Mô tả |
| --- | --- | --- | --- |
| POST | `/orders/{id}/review` | Customer | Đánh giá (`BR-12`) |
| POST | `/orders/{id}/dispute` | Customer | Khiếu nại trong 2 giờ (`BR-17`) |
| GET | `/me/disputes` | Customer | |
| GET | `/me/favorites` | Customer | |
| POST | `/me/favorites/{storeId}` | Customer | |
| DELETE | `/me/favorites/{storeId}` | Customer | |
| GET | `/me/notifications` | Đăng nhập | Lọc `unreadOnly` |
| PATCH | `/me/notifications/{id}/read` | Đăng nhập | |

```json
// POST /orders/{id}/review
{ "rating": 5, "comment": "Bánh còn tươi, đáng tiền", "photoUrl": "/uploads/reviews/x.jpg" }

// POST /orders/{id}/dispute
{ "reason": "WrongCategory", "description": "Đặt túi chay nhưng nhận túi có thịt",
  "photoUrl": "/uploads/disputes/y.jpg" }
```

`reason` ∈ `Spoiled` · `WrongCategory` · `Missing` · `Other`. `photoUrl` **bắt buộc**.

---

## 6. Nhân viên & quản lý chi nhánh — `/staff`

Yêu cầu role `StoreStaff` **hoặc** `BrandManager`. Phạm vi kiểm theo `BR-10` / `BR-11`.

| Method | Đường dẫn | Mô tả |
| --- | --- | --- |
| GET | `/staff/sale-batches` | Đợt bán của chi nhánh mình, lọc `status`, `date` |
| POST | `/staff/sale-batches` | Tạo đợt ở trạng thái `Draft` (bước 1–3) |
| PUT | `/staff/sale-batches/{id}` | Sửa — chịu ràng buộc `OQ-14` |
| POST | `/staff/sale-batches/{id}/activate` | Bước 4 — `Draft → Scheduled` |
| POST | `/staff/sale-batches/{id}/cancel` | **Chỉ BrandManager** (`BR-23`) |
| GET | `/staff/sale-batches/{id}/orders` | Đơn phải trả trong đợt — màn hình nóng nhất |
| POST | `/staff/sale-batches/{id}/offers` | Thêm offer từ template vào đợt |
| PUT | `/staff/offers/{id}` | Sửa offer |
| PATCH | `/staff/offers/{id}/quantity` | Điều chỉnh số lượng (`FR-S03`) |
| DELETE | `/staff/offers/{id}` | Xoá — `409 OFFER_HAS_ORDERS` nếu đã có đơn (`BR-24`) |
| POST | `/staff/redemptions` | **Đối soát QR** (`FR-S05`) |
| POST | `/staff/orders/{id}/cancel` | Huỷ đơn do sự cố (`FR-S07`, `BR-16`) |

### 6.1. `POST /staff/sale-batches`

```json
{
  "name": "Xả bánh tối thứ Năm",
  "saleStartTime":   "2026-09-17T12:30:00Z",
  "saleEndTime":     "2026-09-17T14:00:00Z",
  "pickupStartTime": "2026-09-17T13:30:00Z",
  "pickupEndTime":   "2026-09-17T14:30:00Z",
  "offers": [
    { "offerTemplateId": "5d2c...", "actualPrice": 35000,
      "totalQuantity": 4, "maxPerCustomer": 3 }
  ]
}
```

Lỗi: `BATCH_TIME_INVALID` (`BR-15`), `BATCH_OVERLAP` (`OQ-11`),
`PRICE_OUT_OF_BAND` (`BR-07`), `BRAND_NOT_APPROVED` (`BR-14`).

### 6.2. `PUT /staff/sale-batches/{id}` — cái bẫy phân quyền theo thời gian

Sau khi có đơn `Paid` đầu tiên, endpoint này trả `409 BATCH_LOCKED` nếu cố:

- hạ `totalQuantity` xuống dưới số đã bán
- đổi `actualPrice`
- đổi **bất kỳ mốc nào trong 4 mốc thời gian**, kể cả lùi `PickupEndTime` về sau

Chỉ còn cho phép **tăng** `totalQuantity`. Lý do cấm lùi `PickupEndTime`:
`01-REQUIREMENTS.md` §9.4.

### 6.3. `POST /staff/redemptions` — đối soát

```json
// request — một trong hai, không cần cả hai
{ "token": "eyJvcmRlcklkIjoi...Qk1n" }
{ "orderCode": "FR-8A3K9MQ2" }        // khi khách hết pin (FR-S06)
```

```json
// 200
{
  "orderId": "0c4d...",
  "orderCode": "FR-8A3K9MQ2",
  "customerName": "Nguyễn Văn An",
  "redeemedAt": "2026-09-17T13:41:22Z",
  "items": [ { "offerName": "Túi mù bánh ngọt", "quantity": 2 } ]
}
```

| Lỗi | Nghĩa |
| --- | --- |
| `QR_INVALID_SIGNATURE` | Mã giả hoặc bị sửa |
| `QR_EXPIRED` | Quá `PickupEndTime` |
| `FORBIDDEN_SCOPE` | Đơn của chi nhánh khác — `detail` nói rõ tên chi nhánh đúng (`BR-10`) |
| `REDEEM_WINDOW_CLOSED` | Tới quá sớm (`BR-08`) |
| `ORDER_ALREADY_REDEEMED` | **Đã quét rồi** (`BR-09`) |

---

## 7. Thương hiệu — `/brand`

Yêu cầu role `BrandManager`. Mọi endpoint ngầm lọc theo `brand_id` trong token.

| Method | Đường dẫn | Mô tả |
| --- | --- | --- |
| GET / PUT | `/brand/profile` | Hồ sơ thương hiệu |
| GET / POST | `/brand/stores` | Danh sách / tạo chi nhánh |
| PUT / DELETE | `/brand/stores/{id}` | Sửa / xoá mềm |
| GET / POST | `/brand/offer-templates` | **Mẫu mặt hàng** (`FR-B04`) |
| PUT / DELETE | `/brand/offer-templates/{id}` | |
| GET / POST | `/brand/staff` | Danh sách / tạo tài khoản nhân viên |
| DELETE | `/brand/staff/{id}` | Gỡ nhân viên khỏi chi nhánh |
| GET | `/brand/stats` | 2 biểu đồ (`FR-B06`), số liệu theo `BR-25` |
| GET | `/brand/offer-templates/import-template` | Tải file Excel mẫu để điền (`FR-B07`) |
| POST | `/brand/offer-templates/import` | Import mẫu mặt hàng từ `.xlsx` (`FR-B07`, `BR-26`) |
| GET | `/brand/orders/export` | Export đơn + thống kê ra `.xlsx`, `?from=&to=` (`FR-B08`, `BR-27`) |

```json
// POST /brand/offer-templates
{
  "name": "Túi mù bánh ngọt",
  "description": "3–5 món bánh ngọt trong ngày",
  "imageUrl": "/uploads/offers/abc.jpg",
  "categoryId": "1a2b...",
  "offerType": "SurpriseBag",
  "originalPrice": 120000,
  "suggestedPrice": 35000,
  "brandMinDiscountPercent": 65,
  "brandMaxDiscountPercent": 75
}
```

Biên độ của brand phải **nằm trong** biên độ của sàn theo `offerType`, nếu không →
`400 PRICE_OUT_OF_BAND` (`BR-07`).

```json
// GET /brand/stats?from=2026-09-01&to=2026-09-30
{
  "totalRevenue": 12450000,        // SUM(StorePayoutAmount), theo BR-25
  "totalRescuedPortions": 342,     // SUM(Quantity) của đơn Redeemed
  "byStore": [
    { "storeId": "b77d...", "storeName": "Nguyễn Huệ",
      "revenue": 5320000, "rescuedPortions": 148,
      "noShowRate": 0.06, "ratingAverage": 4.6 }
  ]
}
```

### 7.1. Import / Export Excel

**`POST /brand/offer-templates/import`** — `multipart/form-data`, field `file` (`.xlsx`).
Cột của file theo đúng thứ tự trong file mẫu tải từ `/import-template`: `name`,
`description`, `imageUrl`, `categorySlug`, `offerType`, `originalPrice`, `suggestedPrice`,
`brandMinDiscountPercent`, `brandMaxDiscountPercent`.

```json
// 200 — mọi dòng hợp lệ, đã nhập hết
{ "importedCount": 25 }

// 400 — có dòng sai, KHÔNG nhập dòng nào (BR-26). Số dòng tính theo Excel, dòng 1 là tiêu đề
{
  "status": 400,
  "errorCode": "VALIDATION_FAILED",
  "errors": {
    "rows[4].originalPrice": ["Giá gốc phải lớn hơn 0"],
    "rows[9].brandMaxDiscountPercent": ["Biên độ vượt khung của sàn cho SurpriseBag (65–75%)"]
  }
}
```

**`GET /brand/orders/export?from=2026-09-01&to=2026-09-30`** và **`GET /admin/orders/export`**
— trả file, không trả JSON:

| Hạng mục | Giá trị |
| --- | --- |
| `Content-Type` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |
| `Content-Disposition` | `attachment; filename="orders_2026-09-01_2026-09-30.xlsx"` |
| Sheet `Orders` | `OrderCode`, `StoreName`, `CreatedAt (UTC+7)`, `Status`, `RefundStatus`, `TotalAmount`, `PlatformFee`, `StorePayoutAmount`, `ItemCount` |
| Sheet `Summary` | Cùng số liệu với `/brand/stats`, theo `BR-25` |

> Endpoint export trả file nên **không** theo `PagedResponse<T>` (§1.1) — đây là ngoại lệ
> duy nhất, vì mục đích là lấy trọn khoảng ngày đã chọn.

---

## 8. Quản trị sàn — `/admin`

Yêu cầu role `SystemAdmin`.

| Method | Đường dẫn | Mô tả |
| --- | --- | --- |
| GET | `/admin/brands` | Lọc `status` |
| POST | `/admin/brands/{id}/approve` | `PendingApproval → Approved` (`OQ-01`) |
| POST | `/admin/brands/{id}/reject` | Kèm lý do |
| POST | `/admin/brands/{id}/suspend` | |
| GET | `/admin/users` | Lọc `role`, `isActive`, `q` |
| PATCH | `/admin/users/{id}/status` | Khoá / mở khoá |
| GET / POST | `/admin/categories` | |
| PUT / DELETE | `/admin/categories/{id}` | |
| GET | `/admin/disputes` | Hàng đợi xử lý, lọc `status` |
| POST | `/admin/disputes/{id}/approve` | **Duyệt → hoàn tiền** (`OQ-25`) |
| POST | `/admin/disputes/{id}/reject` | Kèm `adminNote` |
| GET | `/admin/orders/export` | Export đơn toàn sàn ra `.xlsx`, `?from=&to=` (`FR-A07`) — khuôn file ở §7.1 |

> Brand Manager và Staff **không** có endpoint duyệt khiếu nại. Họ chỉ ghi giải trình
> qua `PATCH /staff/disputes/{id}/merchant-note` — tránh xung đột lợi ích (`OQ-25`).

---

## 9. OData — Ch.04

Ngoài `/api/v1`, không versioning, chỉ đọc, chỉ dành cho dữ liệu công khai.

| Đường dẫn | Thực thể |
| --- | --- |
| `GET /odata/Stores` | Chi nhánh đang hoạt động |
| `GET /odata/Offers` | Offer thuộc đợt bán `Active` |

Hỗ trợ: `$filter` `$orderby` `$select` `$expand` `$top` `$skip` `$count`.
Giới hạn `$top` tối đa 100; mặc định `$top=20` khi không truyền.

```http
GET /odata/Offers?$filter=offerType eq 'SurpriseBag' and actualPrice le 40000
    &$orderby=actualPrice asc&$expand=store($select=name,district)&$top=10&$count=true
```

> **Không nhét OData vào các endpoint có nghiệp vụ.** Đặt hàng, đối soát, thống kê đều
> phải qua `/api/v1` — OData chỉ để tra cứu danh mục. Trộn hai thứ sẽ khiến việc phân
> quyền theo phạm vi (`BR-10`, `BR-11`) gần như không kiểm soát nổi.

---

## 10. SignalR — `/hubs/offers`

Kết nối kèm access token qua query string: `/hubs/offers?access_token=<jwt>`.

**Client gọi server:**

| Method | Tham số | Mô tả |
| --- | --- | --- |
| `JoinBatch` | `saleBatchId` | Vào nhóm theo dõi một đợt bán |
| `LeaveBatch` | `saleBatchId` | |
| `JoinStore` | `storeId` | Theo dõi mọi đợt bán của một chi nhánh |

**Server bắn về client:**

| Sự kiện | Payload | Khi nào |
| --- | --- | --- |
| `OfferQuantityChanged` | `{ offerId, saleBatchId, remainingQuantity }` | Tồn kho đổi (`FR-X06`) |
| `SaleBatchActivated` | `{ saleBatchId, storeId, storeName, offerCount }` | `FR-X01` |
| `SaleBatchClosed` | `{ saleBatchId }` | `FR-X02` |
| `OrderStatusChanged` | `{ orderId, status }` | Gửi riêng cho chủ đơn |

> `OfferQuantityChanged` là **thông báo gợi ý, không phải nguồn sự thật**. Client không
> được dựa vào nó để quyết định còn hàng hay không — tin nhắn có thể tới trễ hoặc mất.
> Nguồn sự thật duy nhất là kết quả của `POST /orders`.

---

## 11. gRPC — QrService (Ch.09, Ch.10)

Tiến trình riêng. **Chỉ dịch vụ này giữ khoá bí mật** (`OQ-22`).

```protobuf
syntax = "proto3";
package freshrescue.qr;

service QrService {
  rpc IssueTicket  (IssueTicketRequest)  returns (IssueTicketReply);
  rpc VerifyTicket (VerifyTicketRequest) returns (VerifyTicketReply);
}

message IssueTicketRequest {
  string order_id  = 1;
  string store_id  = 2;
  int64  expires_at_unix = 3;
}
message IssueTicketReply { string token = 1; }

message VerifyTicketRequest { string token = 1; }
message VerifyTicketReply {
  bool   valid     = 1;
  string order_id  = 2;
  string store_id  = 3;
  int64  expires_at_unix = 4;
  string error_code = 5;   // QR_INVALID_SIGNATURE | QR_EXPIRED
}
```

`VerifyTicket` **chỉ kiểm chữ ký và hạn dùng**. Việc kiểm phạm vi chi nhánh (`BR-10`),
khung giờ đối soát (`BR-08`) và tính idempotent (`BR-09`) do API đảm nhiệm — QrService
không truy cập cơ sở dữ liệu.

---

## 12. Upload ảnh

| Method | Đường dẫn | Quyền |
| --- | --- | --- |
| POST | `/uploads/images` | Đăng nhập |

`multipart/form-data`, field `file`, thêm `folder` ∈ `offers` · `reviews` · `disputes` ·
`avatars` · `logos`. Chỉ nhận `.jpg` `.jpeg` `.png` `.webp`, tối đa **5 MB**.

```json
// 201
{ "url": "/uploads/offers/9f2a-3b1c.jpg", "sizeBytes": 248310 }
```

Trả **đường dẫn tương đối** (`OQ-09`). Client tự ghép với host — để khi đổi host hoặc
chuyển sang CDN thì dữ liệu cũ không hỏng.

---

## 13. Truy vết yêu cầu → endpoint

| Yêu cầu | Endpoint |
| --- | --- |
| `FR-C02` cửa hàng gần tôi | `GET /stores/nearby`, `GET /odata/Stores` |
| `FR-C03` lọc offer | `GET /offers/active`, `GET /odata/Offers` |
| `FR-C05` đặt hàng | `POST /orders` |
| `FR-C07` vé QR | `POST /orders/{id}/payment`, `GET /orders/{id}/qr` |
| `FR-C10` huỷ đơn | `POST /orders/{id}/cancel` |
| `FR-C11` `FR-C15` đánh giá, khiếu nại | `POST /orders/{id}/review`, `/dispute` |
| `FR-S02` tạo đợt bán 4 bước | `POST /staff/sale-batches` + `/activate` |
| `FR-S05` đối soát | `POST /staff/redemptions` |
| `FR-S07` cửa hàng huỷ đơn | `POST /staff/orders/{id}/cancel` |
| `FR-B04` mẫu mặt hàng | `/brand/offer-templates` |
| `FR-B06` thống kê | `GET /brand/stats` |
| `FR-A01` duyệt thương hiệu | `POST /admin/brands/{id}/approve` |
| `FR-A04` duyệt khiếu nại | `POST /admin/disputes/{id}/approve` |
| `FR-B07` import mẫu mặt hàng | `POST /brand/offer-templates/import` |
| `FR-B08` `FR-A07` export đơn | `GET /brand/orders/export`, `GET /admin/orders/export` |
| `FR-X06` realtime | Hub `OfferQuantityChanged` |

---

## 14. Việc còn lại trước khi đóng băng

- [ ] Sinh Postman collection từ Swagger, dùng cho demo và báo cáo (Tuần 5)
- [ ] Bổ sung ví dụ response cho từng endpoint trong Swagger XML comment
- [ ] Rà lại: mọi endpoint danh sách đã có `page`/`pageSize` và `PagedResponse<T>` chưa
- [ ] Rà lại: mọi `DateTime` trả về đã có hậu tố `Z` chưa
