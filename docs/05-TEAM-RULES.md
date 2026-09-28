# FreshRescue — Luật code & Git cho nhóm PRN232

> Kế hoạch: `00-PLAN.md` · Contract: `03-API-CONTRACT.md`
> Cập nhật: 2026-09-17 · Áp dụng cho cả 6 thành viên
>
> Tài liệu này ngắn có chủ đích. Luật dài không ai đọc, và luật không ai đọc thì
> không phải luật.

---

## 0. Mười điều cấm

Đọc thuộc phần này là đủ tránh 90% tai nạn.

| # | Cấm | Vì sao |
| --- | --- | --- |
| 1 | Commit thẳng vào `main` hoặc `dev` | Mọi thứ đi qua Pull Request |
| 2 | `git push --force` lên `main` / `dev` | Xoá công của người khác, không lấy lại được |
| 3 | Tự chạy `Add-Migration` | **Chỉ Tùng.** Xem §3 |
| 4 | Sửa file migration đã lên `dev` | Người khác đã chạy nó rồi |
| 5 | Dùng `DateTime.Now` | Toàn hệ thống là UTC. Chỉ `DateTime.UtcNow` |
| 6 | Trả `Entity` thẳng từ Controller | Luôn trả DTO |
| 7 | Gọi `DbContext` trong Controller | Controller → Service → Repository |
| 8 | `.Result` / `.Wait()` / `async void` | Khoá luồng, nuốt lỗi |
| 9 | Commit `bin/`, `obj/`, `.vs/`, chuỗi kết nối thật | Xem §2.4 |
| 10 | Sửa code trong lát của người khác mà không hỏi | Xem §5.1 |

---

## 1. Nhánh

```
main   ← chỉ nhận merge từ dev ở các mốc tuần. Luôn chạy được.
 └ dev ← nhánh tích hợp. Mọi PR đổ về đây.
    ├ feature/tung-order-concurrency
    ├ feature/hieu-background-services
    └ fix/tv3-odata-filter-null
```

**Đặt tên:** `<loại>/<tên>-<mô-tả-ngắn>`
Loại: `feature` · `fix` · `docs` · `chore`
Mô tả viết không dấu, nối bằng gạch ngang. Ngắn thôi — tên nhánh không phải mô tả công việc.

```
✅ feature/tung-jwt-custom-claims
✅ fix/tv4-store-list-paging
❌ feature/sualaicaidonhang
❌ tung
```

**Một nhánh = một việc.** Xong thì merge và xoá. Nhánh sống quá 3 ngày là dấu hiệu
việc bị chia quá to.

---

## 2. Commit

### 2.1. Khuôn

```
<loại>(<phạm vi>): <mô tả ngắn, tiếng Việt có dấu>

<thân bài, tuỳ chọn — giải thích VÌ SAO, không phải LÀM GÌ>
```

Loại: `feat` · `fix` · `refactor` · `test` · `docs` · `chore`
Phạm vi: `auth` `order` `offer` `batch` `qr` `odata` `signalr` `web` `db` `infra`

```
✅ feat(order): thêm optimistic locking khi đặt hàng
✅ fix(qr): quét lần hai trả 409 thay vì trừ hàng lần nữa
✅ docs(contract): bổ sung mã lỗi BATCH_OVERLAP
❌ update code
❌ fix bug
❌ asdasd
```

### 2.2. Kích thước

Một commit = một thay đổi có nghĩa. Đừng gom "sửa 5 thứ không liên quan" vào một commit,
cũng đừng commit 20 lần cho một dòng.

### 2.3. Tần suất

**Đẩy lên ít nhất một lần mỗi ngày làm việc.** Code nằm trên máy cá nhân là code không
tồn tại — không ai review được, không ai biết bạn tới đâu, và máy hỏng là mất sạch.

### 2.4. Không bao giờ commit

```gitignore
# .gitignore — bản tối thiểu
bin/
obj/
.vs/
*.user
*.suo
appsettings.Development.json      # chuỗi kết nối riêng của từng người
wwwroot/uploads/*                 # ảnh upload lúc chạy thử
!wwwroot/uploads/.gitkeep
```

Chuỗi kết nối để ở `appsettings.Development.json` (đã bị bỏ qua). Trong repo commit
`appsettings.Development.json.example` để người mới copy ra rồi sửa.

> Nếu lỡ commit chuỗi kết nối hoặc khoá bí mật: **báo Tùng ngay**, đừng tự xoá bằng
> commit mới — nó vẫn nằm trong lịch sử.

---

## 3. Migration — luật nghiêm nhất

**Chỉ Tùng chạy `Add-Migration`.**

Đây không phải chuyện phân cấp mà là chuyện kỹ thuật: mỗi migration ghi lại
`ModelSnapshot`, và khi 6 người cùng sinh migration thì file snapshot xung đột theo kiểu
gần như không sửa tay được. Gỡ một mớ migration rối mất cả ngày — bằng đúng thời gian
dự phòng còn lại của cả dự án.

**Cần đổi schema thì làm thế này:**

1. Sửa entity trong `FreshRescue.Entities` và Fluent config trong `DataAccess`
2. **Không** chạy `Add-Migration`
3. Báo Tùng (kèm PR hoặc nhắn rõ đổi gì)
4. Tùng sinh migration, đẩy lên `dev`
5. Cả nhóm `git pull` rồi chạy `Update-Database`

**Khi kéo về thấy có migration mới:**

```bash
git pull --rebase origin dev
dotnet ef database update --project src/FreshRescue.DataAccess --startup-project src/FreshRescue.API
```

Quên bước hai là app chạy lỗi với thông báo rất khó hiểu. Gặp lỗi lạ sau khi pull thì
việc đầu tiên là chạy `Update-Database`.

---

## 4. Pull Request

### 4.1. Điều kiện để mở PR

- [ ] Build sạch, **không có warning mới**
- [ ] Đã tự chạy thử luồng mình làm
- [ ] Không còn `Console.WriteLine` gỡ lỗi, không còn code bị comment
- [ ] Đã `git pull --rebase origin dev` và xử lý xung đột

### 4.2. Mô tả PR

```markdown
## Làm gì
Thêm luồng đặt hàng có chống over-selling.

## Liên quan
FR-C05, BR-01, BR-03, BR-06, NFR-01

## Đã kiểm thử thế nào
- Đặt hàng bình thường → 201
- 50 request đồng thời mua 10 suất → đúng 10 đơn, 40 lỗi 409
- Đặt quá 3 suất → PURCHASE_LIMIT_EXCEEDED

## Cần lưu ý khi review
Chỗ sắp xếp offer theo Id trong `OrderService` là để tránh deadlock, đừng bỏ.
```

Mục **Liên quan** bắt buộc ghi mã `FR-xx` / `BR-xx`. Không tra được về yêu cầu nào
nghĩa là đang làm thứ không ai yêu cầu.

### 4.3. Review

- **Tùng hoặc Hiếu duyệt** mọi PR. Hai người duyệt PR của nhau.
- Duyệt trong vòng **24 giờ**. PR nằm chờ là cả nhánh phía sau đứng im.
- PR quá **400 dòng thay đổi** thì tách nhỏ. PR to sẽ được duyệt bằng cách bấm
  "Approve" mà không đọc — và như thế review thành nghi thức rỗng.

### 4.4. Merge

Dùng **Squash and merge** vào `dev`. Lịch sử `dev` sạch, mỗi PR một dòng.
Merge xong xoá nhánh.

---

## 5. Luật code

### 5.1. Sở hữu theo lát dọc

Mỗi người sở hữu trọn bộ controller + service + DTO của phần mình (`00-PLAN.md` §5).

- Cần đổi code trong lát của người khác → **nhắn trước**, đừng sửa lặng lẽ
- Thấy code người khác có vẻ sai → hỏi, đừng "sửa giúp". `02-DOMAIN-MODEL.md` §7 liệt kê
  những chỗ **cố tình** phi chuẩn hoá; "sửa giúp" ở đó là làm hỏng
- Code dùng chung (`Entities`, `DataAccess`, middleware, `BaseController`) → Tùng duyệt

### 5.2. Chiều phụ thuộc — không có ngoại lệ

```
API → Business → DataAccess → Entities
WebClient ──HTTP──► API          (KHÔNG reference DataAccess)
```

Thấy mình cần vi phạm chiều này nghĩa là logic đang đặt sai chỗ.

### 5.3. Controller mỏng

```csharp
// ✅
[HttpPost]
public async Task<IActionResult> Create(CreateOrderRequest req, CancellationToken ct)
{
    var result = await _orderService.CreateAsync(req, User.GetUserId(), ct);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

Controller chỉ làm: nhận request → gọi service → trả status code.
**Không** `if` nghiệp vụ, **không** `DbContext`, **không** `try/catch` (đã có middleware).

### 5.4. Đặt tên

| Thứ | Quy ước | Ví dụ |
| --- | --- | --- |
| Interface | `I` + danh từ | `IOrderService` |
| Service | `<Miền>Service` | `OrderService` |
| DTO vào | `<Hành động><Miền>Request` | `CreateOrderRequest` |
| DTO ra | `<Miền>Dto` / `<Miền>DetailDto` | `OrderDto` |
| Phương thức bất đồng bộ | hậu tố `Async` | `CreateAsync` |
| Trường private | `_camelCase` | `_unitOfWork` |
| Hằng số | `PascalCase` | `MaxPerCustomerDefault` |

Một class một file. Tên file trùng tên class.

### 5.5. Bất đồng bộ

Mọi thao tác chạm CSDL hoặc mạng đều `async`, và **truyền `CancellationToken` xuyên
suốt**. Không `.Result`, không `.Wait()`, không `async void` (trừ event handler).

### 5.6. Sáu quy tắc gắn với miền nghiệp vụ

Sáu điều này đã có lý do viết sẵn trong tài liệu — đọc trước khi động vào:

| Quy tắc | Tài liệu |
| --- | --- |
| Trong giao dịch, luôn xử lý `Offer` **theo `Id` tăng dần** | `02-DOMAIN-MODEL.md` §6.2 — chống deadlock |
| Hoàn tồn kho phải kèm `UPDATE ... WHERE Status = @cũ` | §6.4 — chống hoàn tồn hai lần |
| Idempotent nằm ở mệnh đề `WHERE`, không phải ở câu `if` | §6.5 |
| Tiền: `PlatformFee` làm tròn trước, `StorePayout = Total − Fee` | `BR-20` |
| Truy vấn lịch sử đơn hàng dùng `IgnoreQueryFilters()` | `BR-22` |
| Enum ra JSON là **chuỗi**, không phải số | `03-API-CONTRACT.md` §1 |

### 5.7. Kiểm tra dữ liệu vs quy tắc nghiệp vụ

| Loại | Đặt ở đâu | Ví dụ |
| --- | --- | --- |
| Định dạng, bắt buộc, khoảng giá trị | FluentValidation, ở biên request | `quantity > 0` |
| Quy tắc nghiệp vụ (`BR-xx`) | Service | `BR-06` giới hạn mua |
| Bất biến của dữ liệu | Ràng buộc CSDL | `CHECK (RemainingQuantity >= 0)` |

Ba tầng, không trùng nhau, không thiếu tầng nào.

### 5.8. Comment

Chỉ viết comment giải thích **vì sao**, không mô tả **cái gì**.

```csharp
// ✅ Sắp theo Id để hai giao dịch đồng thời khoá hàng theo cùng thứ tự (xem BR-01)
var offers = requested.OrderBy(x => x.OfferId).ToList();

// ❌ Sắp xếp danh sách offer
var offers = requested.OrderBy(x => x.OfferId).ToList();
```

`TODO` phải có tên và ngày: `// TODO(Hieu, 2026-10-05): thay bằng VNPay thật`.
Không tên không ngày thì mãi mãi không ai làm.

---

## 6. Nhịp làm việc

| Khi nào | Việc |
| --- | --- |
| Mỗi ngày | Đẩy code lên ít nhất một lần |
| Mỗi ngày | Nhắn nhóm 3 dòng: hôm qua làm gì, hôm nay làm gì, đang vướng gì |
| Trong 24h | Duyệt PR được gán cho mình |
| Cuối mỗi tuần | Đối chiếu mốc chốt trong `00-PLAN.md` §3 |
| **13/10** | **Phong toả tính năng** — từ đây chỉ `fix`, không `feat` |

**Vướng quá 2 tiếng thì hỏi.** Ngồi một mình với một lỗi suốt buổi tối là cách tiêu
tiền ngân sách nhanh nhất — mà ngân sách chỉ còn dư 1.5 person-day.

---

## 7. Xong là xong đến đâu

Một việc chỉ tính là xong khi đủ cả 6:

1. Code chạy đúng luồng chính **và** các nhánh lỗi
2. Trả đúng HTTP status code và `errorCode` như `03-API-CONTRACT.md`
3. Hiện đúng trên Swagger, bấm thử được
4. Không có warning mới lúc build
5. PR đã được duyệt và merge vào `dev`
6. Contract đã cập nhật nếu có đổi endpoint hoặc DTO

---

## 8. Sổ tay Git cho tình huống hay gặp

```bash
# Bắt đầu một việc mới
git checkout dev && git pull origin dev
git checkout -b feature/tung-order-concurrency

# Đẩy lần đầu
git push -u origin feature/tung-order-concurrency

# dev đã chạy trước, kéo về cho khớp
git pull --rebase origin dev
# ...xử lý xung đột nếu có...
git rebase --continue

# Lỡ commit vào dev (chưa push)
git reset --soft HEAD~1
git stash
git checkout -b feature/ten-nhanh
git stash pop

# Xem mình đã đổi gì so với dev
git diff dev...HEAD --stat
```

**Gặp xung đột ở file migration hoặc `ModelSnapshot`: dừng lại, gọi Tùng.**
Đừng tự chọn "giữ bên tôi" hay "giữ bên kia" — cả hai đều sai, và hậu quả chỉ lộ ra khi
chạy `Update-Database` ở máy người khác.
