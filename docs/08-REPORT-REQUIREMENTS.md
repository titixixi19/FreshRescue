# FreshRescue — Yêu cầu báo cáo cuối kỳ

> Nguồn: file "PRN232_Báo cáo Project_Fall2026" và mẫu "Slot 6 — Create database schema
> from ERD" của giảng viên · Kế hoạch: `00-PLAN.md` §1.3 · Phân công: `07-WORK-BREAKDOWN.md`
> Cập nhật: 2026-09-29

Có hai sản phẩm phải nộp: **báo cáo Word** và **slide thuyết trình** (tóm tắt báo cáo Word).
Điểm thuyết trình gồm phần chuẩn bị slide và phần trình bày.

---

## 1. Ba điều bắt buộc dễ bị quên

| # | Yêu cầu | Hệ quả với dự án |
| --- | --- | --- |
| 1 | **Báo cáo viết bằng tiếng Anh** | Tài liệu nội bộ bằng tiếng Việt chỉ là nguyên liệu; khi viết phải dịch. Code không đổi |
| 2 | **Mỗi bảng ít nhất 20 bản ghi**, kèm ảnh chụp thiết kế và dữ liệu của từng bảng | `NFR-09` — seeder phải sinh đủ (`T4-TV3-03`, `T5-TV6-02`) |
| 3 | Chức năng phải có: CRUD, đăng nhập / đăng ký / reset password, **Import/Export file**, search, sort (sort by, then by, order by), tăng cường bảo mật | Import/Export là `FR-B07`, `FR-B08`, `FR-A07`. Sort "then by" dùng OData `$orderby=actualPrice asc,name desc` |

---

## 2. Khung báo cáo và người phụ trách

Thứ tự mục theo đúng khung của cô. **Hiếu** tổng hợp, thống nhất giọng văn và làm mục lục.

| Mục | Nội dung cô yêu cầu | Người viết | Nguyên liệu có sẵn |
| --- | --- | --- | --- |
| Trang 1 | Bìa chính: tên đề tài, tên nhóm, tên thành viên | Hiếu | |
| Trang 2 | Bìa phụ: như bìa chính | Hiếu | |
| Trang 3 | **Kế hoạch từ tuần 1 đến tuần 9** | Tùng | `00-PLAN.md` §3 — *cần hỏi cô về tuần 7–9, xem `00-PLAN.md` §1.3* |
| Trang 4 | **Phân công chi tiết từng thành viên** | Tùng | `07-WORK-BREAKDOWN.md` §1, §4–§8 |
| Trang 5 | Mục lục | Hiếu | Sinh tự động trong Word |
| Mục 6 | Danh mục hình ảnh | Hiếu | Chèn caption cho mọi ảnh để Word tự sinh |
| Mục 7 | Danh mục bảng | Hiếu | Như trên |
| **Phần 1** | | | |
| 1.1 | Giới thiệu bài toán; danh sách chức năng: CRUD, đăng nhập / đăng ký / reset password, Import/Export, search, sort, bảo mật | TV4 (bài toán) + TV5 (chức năng) | `01-REQUIREMENTS.md` §1–§6 |
| 1.2.1 | **ERD** — ảnh + entity mạnh / yếu + thuộc tính từng entity + danh sách quan hệ | Tùng | `02-DOMAIN-MODEL.md` §2–§3. **Phải vẽ lại theo kiểu Chen** — xem §3 dưới đây |
| 1.2.2 | **Chuyển ERD → database schema** theo quy tắc, trình bày giống mẫu Slot 6 | Tùng | §3 dưới đây |
| 1.2.3 | Liệt kê các bảng + **ảnh thiết kế và ảnh dữ liệu** từng bảng (≥ 20 bản ghi) | TV3 | Chụp từ SSMS sau khi seed |
| **Phần 2** | | | |
| 2.x | **Mỗi nhóm API một mục**: URL, method, tham số, kiểu trả về, ảnh Swagger (xem §4) | Chủ lát dọc tự viết API của mình | `03-API-CONTRACT.md` |
| 2.x | — Xác thực (`/auth`) | Hiếu | `03` §2 |
| 2.x | — Danh mục, thương hiệu, quản trị, OData, Import mẫu hàng | TV3 | `03` §3, §7, §8, §9 |
| 2.x | — Đợt bán, gRPC QrService | TV6 | `03` §6, §11 |
| 2.x | — Đặt hàng, thanh toán, đối soát QR — **kèm phần chống over-selling có số liệu load test** | Tùng | `03` §4, §6.3; `02` §6 |
| 2.x | — Export Excel, SignalR, tiến trình nền | Hiếu | `03` §7.1, §10 |
| 2.3.1 | Front-end ASP.NET Core Web: các chức năng | TV4 (khách) + TV5 (dashboard) | |
| 2.3.2 | Front-end: cách gọi API (HttpClient, fetch/AJAX, SignalR JS) | TV4 + TV5 | |
| **Phần 10** | Demo và deploy dự án | TV6 (deploy) + Tùng (kịch bản demo) | |
| **Phần 11** | Đánh giá: hạn chế + hướng phát triển | Hiếu | `01-REQUIREMENTS.md` §10, `00-PLAN.md` §1.1 (những gì đã cắt) |
| **Phần 12** | Tài liệu tham khảo: đường link, tài liệu đã dùng | Hiếu | Mỗi người gửi Hiếu link mình đã dùng |
| **Slide** | Tóm tắt toàn bộ báo cáo Word | TV5 | |

> Khung của cô không có chương riêng cho concurrency, gRPC hay SignalR — nhưng đó là phần ăn
> điểm nhất (`00-PLAN.md` §4). Đặt chúng **bên trong** mục API tương ứng ở Phần 2, và cho
> vào kịch bản demo ở Phần 10.

---

## 3. ERD kiểu Chen và chuyển sang schema — theo mẫu Slot 6

Mẫu của cô dùng **ký hiệu Chen**: entity là hình chữ nhật, thuộc tính là hình bầu dục, quan
hệ là hình thoi, entity yếu có viền đôi. Sơ đồ trong `02-DOMAIN-MODEL.md` §2 dùng ký hiệu
crow's foot — **không dùng được nguyên xi**, phải vẽ lại (draw.io có sẵn bộ hình Chen).

### 3.1. Phân loại entity

| Loại | Entity | Ghi chú khi trình bày |
| --- | --- | --- |
| **Mạnh** | `AppUser`, `Brand`, `Store`, `Category`, `OfferTemplate`, `SaleBatch`, `Offer`, `Order` | Tồn tại độc lập |
| **Yếu** | `OrderItem`, `QrTicket`, `Payment`, `Review`, `Dispute` (chủ: `Order`) · `RefreshToken`, `Notification` (chủ: `AppUser`) | Không tồn tại nếu không có entity chủ. Trong CSDL vẫn có khoá `Guid` riêng — giải thích trong báo cáo rằng khoá đó là khoá thay thế, còn về ngữ nghĩa chúng phụ thuộc entity chủ |

### 3.2. Các loại thuộc tính có trong mô hình — khớp với mẫu Slot 6

| Loại thuộc tính (mẫu Slot 6) | Ví dụ trong FreshRescue |
| --- | --- |
| Key attribute | `Id` của mọi entity; `Brand.Slug`, `Order.OrderCode` là khoá ứng viên |
| Composite attribute | **Địa chỉ `Store`** gồm `AddressLine`, `Ward`, `District`, `City` → không có cột "Address", tách 4 cột (giống `Name` → `Fname`, `Minit`, `Lname` trong mẫu) |
| Multivalued attribute | **Vai trò của `AppUser`** (một user nhiều role, `OQ-03`) → tách thành bảng `AspNetUserRoles` |
| Derived attribute | `Store.RatingAverage`, `Store.RatingCount` (suy ra từ `Review`), `Order.TotalAmount` (suy ra từ `OrderItem`). Vẫn lưu thành cột — lý do ở `02-DOMAIN-MODEL.md` §7 (giống `NumberOfEmployees` trong mẫu) |

### 3.3. Các quan hệ

| Quan hệ | Bậc | Chuyển sang schema |
| --- | --- | --- |
| `Brand` — có — `Store` | 1–N | Không tạo bảng; `Store.BrandId` là khoá ngoại |
| `AppUser` — sở hữu — `Brand` | 1–1 | Không tạo bảng; `Brand.OwnerUserId` khoá ngoại + unique |
| `Store` — mở — `SaleBatch` | 1–N | Không tạo bảng; `SaleBatch.StoreId` |
| `SaleBatch` — chứa — `Offer` | 1–N | Không tạo bảng; `Offer.SaleBatchId` |
| `OfferTemplate` — sinh ra — `Offer` | 1–N | Không tạo bảng; `Offer.OfferTemplateId` |
| `Category` — phân loại — `OfferTemplate` | 1–N | Không tạo bảng; `OfferTemplate.CategoryId` |
| `AppUser` — đặt — `Order` | 1–N | Không tạo bảng; `Order.CustomerUserId` |
| `Order` — cấp vé — `QrTicket` | 1–1 | Không tạo bảng; `QrTicket.OrderId` khoá ngoại + unique |
| **`Order` — mua — `Offer`** | **M–N**, có thuộc tính `Quantity`, `UnitPrice` | **Tạo bảng `OrderItem`** — giống `Works_On` (thuộc tính `Hours`) trong mẫu |
| **`AppUser` — yêu thích — `Store`** | **M–N** | **Tạo bảng `FavoriteStore`**, khoá chính tổ hợp (`CustomerUserId`, `StoreId`) |
| `AppUser` — làm việc tại — `Store` | 1–N, có thuộc tính `Position`, `IsActive` | Tạo bảng `StoreStaff` để giữ thuộc tính của quan hệ; `UserId` unique vì 1 nhân viên 1 chi nhánh (`OQ-07`) |

---

## 4. Cách trình bày một API — theo mẫu của cô

Mẫu của cô trình bày mỗi API bằng: URL, method, tham số nhận vào, mô tả dữ liệu trả về, rồi
**ảnh chụp Swagger** gồm Request URL, mã phản hồi và response body. Mỗi người làm như sau
cho từng endpoint của mình:

```text
The API at url /api/v1/orders:
  - Receives a JSON body: saleBatchId (Guid), items (list of offerId + quantity).
  - Using POST method. Requires role Customer.
  - Returns 201 with the created order (OrderDto).
  - Returns 409 OFFER_SOLD_OUT when stock runs out, PURCHASE_LIMIT_EXCEEDED over 3 portions.
  [Ảnh Swagger: Request URL + Server response + Response body]
```

Chụp ảnh Swagger **trên dữ liệu seed thật**, không dùng dữ liệu gõ tay lúc chụp.

---

## 5. Checklist trước khi nộp

- [ ] Toàn bộ báo cáo bằng tiếng Anh
- [ ] Đủ các mục theo đúng thứ tự ở §2, có mục lục, danh mục hình, danh mục bảng
- [ ] Mọi ảnh và bảng có caption (để Word tự sinh danh mục)
- [ ] ERD vẽ kiểu Chen, có bảng chuyển ERD → schema
- [ ] Mỗi bảng có ảnh thiết kế + ảnh dữ liệu, **≥ 20 bản ghi**
- [ ] Mỗi API có URL, method, tham số, kiểu trả về, ảnh Swagger
- [ ] Có Import và Export chạy được trong phần demo
- [ ] Có mục deploy với đường dẫn chạy được
- [ ] Slide tóm tắt đủ các phần của báo cáo Word
