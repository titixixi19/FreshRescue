# FreshRescue

Sàn B2B2C **giải cứu thực phẩm**: cửa hàng F&B bán rẻ đồ dư cuối ngày dưới dạng
**túi mù** (giảm 65–75%) hoặc **món lẻ xả kho** (giảm 40–50%). Khách tự đến lấy trong
khung giờ hẹp, đối soát bằng **mã QR**.

Đồ án môn **PRN232 — Building Cross-Platform Back-End Application with .NET**.
Nhóm 6 thành viên · Nộp **20/10/2026**.

---

## Trạng thái

| | |
| --- | --- |
| Tài liệu thiết kế | ✅ Xong (Tuần 2) |
| File cấu hình đồng nhất | ✅ Xong |
| Scaffold solution | ✅ Xong (28/09) — 7 project, build + test sạch |
| Data layer + Auth | ⬜ Tuần 3 |
| Nghiệp vụ lõi | ⬜ Tuần 4 |
| Tính năng rubric | ⬜ Tuần 5 |
| Test + tài liệu + nộp | ⬜ Tuần 6 |

**Mới có bộ khung.** Chưa có entity, migration hay endpoint nghiệp vụ.

### Cổng chạy local

| Project | Địa chỉ |
| --- | --- |
| `FreshRescue.API` | `https://localhost:7001/swagger` |
| `FreshRescue.WebClient` | `https://localhost:7002` |
| `FreshRescue.QrService` (gRPC) | `https://localhost:7003` |

### Ai viết vào đâu

`Program.cs` của API đã **đóng băng**. Mỗi người đăng ký phần của mình trong file riêng:

| Người | File đăng ký |
| --- | --- |
| Tùng | `API/Extensions/ApiLayerExtensions.cs`, `DataAccess/DependencyInjection.cs` |
| Hiếu | `API/Extensions/AuthExtensions.cs`, `SwaggerExtensions.cs`, `RealtimeExtensions.cs`, `BackgroundJobExtensions.cs` |
| TV3 | `API/Extensions/ODataExtensions.cs` |
| TV6 | `API/Extensions/QrServiceClientExtensions.cs` |
| Tất cả | `Business/DependencyInjection.cs` — thêm dòng vào đúng nhóm của mình |

---

## Công nghệ

.NET 8 · ASP.NET Core Web API · EF Core 8 (Code-First) · SQL Server ·
Identity + JWT + Refresh Token · OData · SignalR · gRPC · ASP.NET Core MVC · xUnit

Kiến trúc N-Tier:

```
API → Business → DataAccess → Entities
WebClient ──HTTP──► API
```

---

## Bắt đầu

```bash
git clone <repo>
cd FreshRescue
git checkout dev

# thiết lập một lần cho máy của bạn
git config commit.template .gitmessage
git config core.autocrlf false
git config pull.rebase true

# chuỗi kết nối riêng
cp src/FreshRescue.API/appsettings.Development.json.example \
   src/FreshRescue.API/appsettings.Development.json
# … sửa ConnectionStrings:Default cho SQL Server của bạn

dotnet restore
dotnet build
dotnet test

# chỉ chạy được sau khi Tùng đẩy Migration #1
dotnet ef database update --project src/FreshRescue.DataAccess --startup-project src/FreshRescue.API

dotnet run --project src/FreshRescue.API
```

Swagger: `https://localhost:7001/swagger`

> `dotnet ef` cần cài một lần: `dotnet tool install -g dotnet-ef --version 8.*`

---

## Tài liệu

Đọc theo thứ tự nếu bạn là người mới.

| File | Nội dung |
| --- | --- |
| [`docs/00-PLAN.md`](docs/00-PLAN.md) | Phạm vi, lịch 5 tuần, phân công, ngân sách, rủi ro |
| [`docs/01-REQUIREMENTS.md`](docs/01-REQUIREMENTS.md) | Tác nhân, từ điển nghiệp vụ, `FR` / `BR` / `NFR`, các quyết định |
| [`docs/02-DOMAIN-MODEL.md`](docs/02-DOMAIN-MODEL.md) | ERD 17 bảng, chỉ mục, máy trạng thái, **thiết kế chống over-selling** |
| [`docs/03-API-CONTRACT.md`](docs/03-API-CONTRACT.md) | ~60 endpoint, DTO, 28 mã lỗi, SignalR, proto — **nguồn sự thật** |
| [`docs/04-OPEN-QUESTIONS.md`](docs/04-OPEN-QUESTIONS.md) | Lý do đằng sau từng quyết định thiết kế |
| [`docs/05-TEAM-RULES.md`](docs/05-TEAM-RULES.md) | Luật Git, nhánh, commit, PR, review |
| [`docs/06-CODING-STANDARDS.md`](docs/06-CODING-STANDARDS.md) | Khuôn mẫu code, cấu trúc thư mục, "một việc — một cách" |
| [`docs/07-WORK-BREAKDOWN.md`](docs/07-WORK-BREAKDOWN.md) | Phân công 6 người theo tuần, mã việc, đường găng |
| [`docs/08-REPORT-REQUIREMENTS.md`](docs/08-REPORT-REQUIREMENTS.md) | Khung báo cáo cuối kỳ của giảng viên, ai viết mục nào, checklist trước khi nộp |

**Người mới vào nhóm:** đọc `docs/05-TEAM-RULES.md` §0 và `docs/06-CODING-STANDARDS.md` §4–§7 trước.

---

## Trước khi mở PR

```bash
dotnet format --verify-no-changes
dotnet build
dotnet test
```

Chi tiết: [`docs/05-TEAM-RULES.md`](docs/05-TEAM-RULES.md) §4.
