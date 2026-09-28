# FreshRescue — Danh sách câu hỏi còn bỏ ngỏ

> ## ✅ ĐÃ CHỐT TOÀN BỘ — 17/09/2026
>
> Bản quyết định rút gọn nằm ở **`01-REQUIREMENTS.md` §9.2**, và đó là nơi tra khi code.
> Giữ file này lại vì nó chứa **bối cảnh và lý do** của từng lựa chọn — thứ mà bảng
> quyết định rút gọn không nói được, và là thứ cần tới khi viết báo cáo hoặc khi có
> người hỏi "sao lại làm thế này".
>
> Hai câu được chốt **khác** với mặc định đề xuất ở đây: `OQ-08` (biên độ giá) và
> `OQ-31` (phạm vi web client) — xem `01-REQUIREMENTS.md` §9.3.

> Cập nhật: 2026-09-17 · Liên quan: `01-REQUIREMENTS.md`
>
> Q-01 → Q-10 đã chốt ở `01-REQUIREMENTS.md` §9. Đây là lớp câu hỏi **thứ hai**, lộ ra
> khi đi sâu vào từng luồng.
>
> **Cách dùng:** trả lời theo mã `OQ-xx`. Mỗi câu đều có **phương án mặc định** — nếu
> không trả lời, nhóm cứ theo mặc định mà làm, không ai phải ngồi chờ.

## Mức độ cản trở

| Ký hiệu | Nghĩa |
| --- | --- |
| 🔴 | **Chặn ERD** — cần chốt trước khi vẽ `02-DOMAIN-MODEL.md` |
| 🟡 | Chặn code Tuần 3–4 |
| 🟢 | Quyết sau cũng được |

---

## A. Tài khoản & onboarding

### 🔴 OQ-01. Thương hiệu gia nhập sàn bằng cách nào?
Chủ brand tự đăng ký trên web rồi Admin duyệt, hay Admin tạo tay toàn bộ?
**Vì sao quan trọng:** quyết định `Brand` có trạng thái `Pending` hay không, và có cần
màn hình đăng ký công khai cho doanh nghiệp hay không.
*Mặc định: chủ brand tự đăng ký → `Pending` → Admin duyệt thành `Approved`.*

### 🔴 OQ-02. Tài khoản Brand Manager sinh ra lúc nào?
Cùng lúc với hồ sơ brand (người đăng ký thành BM luôn), hay Admin tạo rồi gán sau?
**Vì sao quan trọng:** quyết định `Brand.OwnerUserId` là bắt buộc hay cho phép rỗng.
*Mặc định: người đăng ký brand trở thành Brand Manager luôn.*

### 🔴 OQ-03. Một người có thể vừa là Customer vừa là Staff không?
Nhân viên cửa hàng cũng là người đi mua túi mù ở chỗ khác.
**Vì sao quan trọng:** nếu có, một user mang nhiều role cùng lúc, và JWT phải chứa
`store_id` nhưng vẫn cho phép đặt hàng. Nếu không, phải chặn ngay từ lúc gán role.
*Mặc định: **có** — một user nhiều role, `store_id` chỉ có nghĩa khi hành động với vai trò Staff.*

### 🟡 OQ-04. Customer đăng ký bằng gì?
Email + mật khẩu, hay số điện thoại + OTP?
**Vì sao quan trọng:** OTP cần dịch vụ SMS (tốn tiền, phải cấu hình) — ảnh hưởng ngân
sách Tuần 3.
*Mặc định: email + mật khẩu, **không** xác thực email. Có "quên mật khẩu" đơn giản.*

---

## B. Cửa hàng

### 🔴 OQ-05. Chi nhánh có giờ mở cửa riêng không?
Đặc tả gốc có nhắc `OpenTime` / `CloseTime` của cửa hàng, tách biệt với giờ của đợt bán.
**Vì sao quan trọng:** thêm 2 cột và thêm ràng buộc (khung giờ nhận hàng có phải nằm
trong giờ mở cửa không?).
*Mặc định: **có** 2 cột, chỉ để hiển thị cho khách, **không** ràng buộc với đợt bán.*

### 🔴 OQ-06. Xoá chi nhánh / mẫu hàng thì dữ liệu cũ ra sao?
Đơn hàng và đợt bán cũ vẫn tham chiếu tới chúng.
**Vì sao quan trọng:** quyết định dùng xoá mềm (`IsActive` / `DeletedAt`) hay xoá cứng.
Xoá cứng sẽ làm vỡ lịch sử đơn hàng.
*Mặc định: **xoá mềm toàn bộ** — `Brand`, `Store`, `OfferTemplate`, `Category`.*

### 🟢 OQ-07. Toạ độ chi nhánh nhập thế nào trên web?
Gõ tay số lat/lng, hay chọn điểm trên bản đồ?
**Vì sao quan trọng:** chọn trên bản đồ cần nhúng Leaflet/Google Maps (~1 pd).
*Mặc định: gõ tay, có nút "xem thử trên bản đồ".*

---

## C. Mẫu mặt hàng (OfferTemplate)

### 🔴 OQ-08. Template chốt giá cứng hay chỉ đưa khung giá?
Câu trả lời Q-01 của bạn có cả hai cách hiểu: *"mẫu Túi Mù Bánh Mặn (giá gốc 100k, giá
giải cứu 35k)"* nghe như giá cứng, nhưng cũng nói *"chuẩn hoá khung giá"*.
**Vì sao quan trọng:** nếu giá cứng thì Staff không được sửa giá lúc tạo đợt, cột giá
nằm ở `OfferTemplate`. Nếu là khung thì phải có `MinPrice`/`MaxPrice` ở template, giá
thật nằm ở `Offer`, và cần validate lúc tạo đợt.
*Mặc định: template ghi **giá gợi ý**, Staff sửa được trong khoảng ±20%, giá thật lưu ở `Offer`.*

### 🟡 OQ-09. Ảnh mặt hàng: upload file hay dán URL?
**Vì sao quan trọng:** upload cần endpoint nhận file + chỗ lưu (wwwroot hay cloud) +
xử lý dung lượng — khoảng 2 pd. Dán URL thì gần như miễn phí nhưng demo kém thuyết phục.
Lưu ý: khiếu nại (`FR-C15`) và đánh giá (`FR-C11`) **cũng cần gửi ảnh**, nên nếu đã làm
upload thì dùng chung được.
*Mặc định: **có upload**, lưu `wwwroot/uploads`, vì dù sao khiếu nại cũng cần.*

### 🟢 OQ-10. Một mẫu hàng có gắn với một chi nhánh cụ thể không?
Hay mọi chi nhánh trong chuỗi đều dùng chung mọi mẫu?
*Mặc định: dùng chung toàn chuỗi — đúng tinh thần "chuẩn hoá" của Q-01.*

---

## D. Đợt bán

### 🔴 OQ-11. Một chi nhánh mỗi ngày được mở mấy đợt bán?
Chỉ một đợt cuối ngày, hay nhiều đợt (trưa + tối)?
**Vì sao quan trọng:** nếu nhiều đợt thì cần chặn trùng khung giờ, và mọi truy vấn
"đợt đang mở" phải trả về danh sách chứ không phải một bản ghi.
*Mặc định: **cho phép nhiều đợt/ngày**, nhưng chặn hai đợt có khung giờ bán chồng nhau.*

### 🔴 OQ-12. Đợt bán có trạng thái nháp không?
Bước 4 trong Q-03 là "bấm Kích hoạt", nghe như trước đó có trạng thái `Draft`.
**Vì sao quan trọng:** quyết định tập trạng thái của `SaleBatch`.
*Mặc định: `Draft → Scheduled → Active → Closed` (+ `Cancelled`). Tạo xong là `Draft`,
bấm kích hoạt thành `Scheduled`, tới `SaleStartTime` tự thành `Active`.*

### 🟡 OQ-13. Có được lên lịch trước không?
Staff tạo lúc 15:00 cho đợt bán 19:30, hay bắt buộc tạo ngay lúc mở bán?
**Vì sao quan trọng:** nếu lên lịch trước thì `FR-X01` mới có việc để làm.
*Mặc định: **có** lên lịch trước — và đây chính là lý do tồn tại của BackgroundService.*

### 🟡 OQ-14. Đã có người đặt rồi, Staff còn sửa được gì?
Sửa khung giờ? Giảm số lượng xuống dưới số đã bán? Đóng đợt sớm khi còn đơn chưa
thanh toán?
**Vì sao quan trọng:** đây là ổ lỗi kinh điển, và cũng là chỗ dễ ăn điểm nếu xử lý khéo.
*Mặc định: sau khi có đơn đầu tiên → **chỉ được tăng số lượng và lùi `PickupEndTime` về
sau**. Đóng đợt sớm thì các đơn `PendingPayment` bị hủy và hoàn tồn kho.*

### 🟢 OQ-15. Hàng không bán hết có ghi nhận không?
Đề tài nhấn mạnh ESG — con số "đã cứu được bao nhiêu suất / còn lãng phí bao nhiêu" là
điểm nhấn rất hợp chủ đề khi demo.
*Mặc định: khi đóng đợt, lưu `UnsoldQuantity` để thống kê.*

---

## E. Đặt hàng & thanh toán

### 🔴 OQ-16. Sàn có thu phí hoa hồng không?
**Vì sao quan trọng:** nếu có thì `Order` cần thêm `PlatformFee` / `StoreEarning`, và
thống kê doanh thu của Brand khác với doanh thu của sàn.
*Mặc định: **có**, phí nền tảng 10%, tính và lưu lúc tạo đơn. Rẻ, và làm phần thống kê
của Admin có ý nghĩa hơn hẳn.*

### 🟡 OQ-17. "Thanh toán mock" cụ thể là gì?
Chỉ một nút "Xác nhận thanh toán" luôn thành công, hay có mô phỏng cả thất bại?
**Vì sao quan trọng:** có nhánh thất bại thì mới demo được luồng hủy đơn.
*Mặc định: màn hình giả có 2 nút — **Thành công** / **Thất bại**. Thất bại thì đơn vẫn
giữ chỗ cho tới khi hết hạn 10 phút.*

### 🟡 OQ-18. Khách đang có đơn chưa thanh toán, có được đặt đơn mới không?
*Mặc định: **được**, nhưng vẫn bị chặn bởi giới hạn cộng dồn `BR-06`.*

### 🟢 OQ-19. Mã đơn hàng định dạng thế nào?
`FR-S06` cho phép nhân viên nhập tay khi khách hết pin — nên mã phải đọc được bằng
miệng, không thể là GUID.
*Mặc định: 8 ký tự chữ + số viết hoa, bỏ các ký tự dễ nhầm (`O`, `0`, `I`, `1`).*

### 🟢 OQ-20. Giá hiển thị đã gồm thuế chưa?
*Mặc định: giá đã gồm tất cả, không tách thuế. Tiền kiểu `decimal(18,0)` — VND không có
phần lẻ.*

---

## F. QR & đối soát

### 🔴 OQ-21. Khách có 2 đơn ở cùng một đợt bán — mấy mã QR?
**Vì sao quan trọng:** ảnh hưởng quan hệ `Order` ↔ `QrTicket` và trải nghiệm ở quầy.
*Mặc định: **mỗi đơn một mã QR** (1–1). Nhân viên quét 2 lần. Đơn giản và khớp `BR-03`.*

### 🟡 OQ-22. Khoá bí mật ký QR nằm ở đâu?
Chỉ `QrService` giữ, hay API cũng biết để tự xác thực mà không cần gọi gRPC?
**Vì sao quan trọng:** nếu chỉ QrService giữ thì mọi lần quét đều phải gọi gRPC — đúng
tinh thần tách dịch vụ, nhưng QrService chết là không đối soát được.
*Mặc định: **chỉ QrService giữ khoá**. Đây chính là lý do chính đáng để gRPC tồn tại,
đừng làm loãng nó đi.*

### 🟡 OQ-23. Nhân viên quét nhầm mã của chi nhánh khác thì sao?
*Mặc định: trả `403` kèm thông báo rõ "Đơn này thuộc chi nhánh X" (`BR-10`).*

### 🟢 OQ-24. Khách tới nơi nhưng cửa hàng thật sự hết đồ thì làm gì?
Dù hệ thống vẫn báo còn. Đây là tình huống có thật.
*Mặc định: nhân viên dùng `FR-S07` hủy đơn + hoàn 100%, bắt buộc nhập lý do.*

---

## G. Khiếu nại & hoàn tiền

### 🔴 OQ-25. Ai duyệt khiếu nại — Admin hay Brand Manager?
Câu trả lời Q-04 của bạn ghi "Admin/Brand Manager". Nhưng để Brand Manager tự duyệt
khiếu nại về chính chuỗi của mình là **xung đột lợi ích** — họ là người mất tiền khi
duyệt.
**Vì sao quan trọng:** quyết định `Dispute` có cột người xử lý thuộc role nào, và phân
quyền endpoint.
*Mặc định: **chỉ Admin duyệt**. Brand Manager được xem và ghi ý kiến phản hồi, nhưng
không tự quyết.*

### 🟡 OQ-26. "Hoàn tiền" với thanh toán mock nghĩa là gì?
Chỉ đổi trạng thái `RefundStatus = Refunded`, hay có ví/số dư ảo để nhìn thấy tiền
quay về?
**Vì sao quan trọng:** ví ảo demo thuyết phục hơn nhiều nhưng tốn thêm ~2 pd.
*Mặc định: chỉ đổi trạng thái + ghi bản ghi `Payment` kiểu `Refund` đối ứng. Không làm ví.*

### 🟢 OQ-27. Khiếu nại bị từ chối, khách có mở lại được không?
*Mặc định: **không** — mỗi đơn đúng một khiếu nại (`BR-17`).*

---

## H. Thông báo & đánh giá

### 🔴 OQ-28. Đánh giá gắn vào đâu — cửa hàng hay từng mặt hàng?
**Vì sao quan trọng:** đổi hẳn khoá ngoại của `Review`, và đổi cách tính điểm trung bình.
*Mặc định: đánh giá **đơn hàng**, quy về điểm của **chi nhánh**. Không đánh giá riêng
từng túi mù — vì bản chất túi mù là ngẫu nhiên, đánh giá nó không có ý nghĩa.*

### 🟡 OQ-29. Thông báo "đợt bán mới" gửi cho ai?
Chỉ người đã bấm yêu thích cửa hàng, hay mọi người trong bán kính X km?
**Vì sao quan trọng:** gửi theo bán kính cần truy vấn địa lý mỗi lần mở đợt — nặng hơn
nhiều so với đọc danh sách yêu thích.
*Mặc định: **chỉ người đã yêu thích**. Bán kính ghi vào "hướng phát triển".*

### 🟢 OQ-30. Điểm trung bình của chi nhánh tính lúc nào?
Tính lại mỗi lần đọc, hay lưu sẵn và cập nhật khi có đánh giá mới?
*Mặc định: lưu sẵn `RatingAverage` + `RatingCount` trên `Store`, cập nhật khi có đánh
giá mới. Danh sách cửa hàng là màn hình được gọi nhiều nhất.*

---

## I. Web client — câu hỏi lớn nhất còn lại

### 🔴 OQ-31. Web client phục vụ ai?
Ba khả năng:

| Phương án | Khối lượng | Ghi chú |
| --- | --- | --- |
| **(a)** Chỉ Admin + Brand Manager + Staff. Khách chỉ dùng app | ~8 pd | Gọn nhất, nhưng giảng viên chấm PRN232 sẽ không thấy luồng khách hàng nếu chưa có app Flutter |
| **(b)** Cả 4 vai trò, khách đặt hàng được trên web | ~13 pd | Demo PRN232 độc lập hoàn toàn, không phụ thuộc app |
| **(c)** Như (b) nhưng phía khách chỉ xem, không đặt | ~10 pd | Nửa vời |

**Vì sao quan trọng:** đây là hạng mục lớn nhất của cả đồ án, và PRN232 nộp **trước**
khi app Flutter xong — nên nếu chọn (a) thì lúc demo sẽ không có cách nào cho giảng viên
thấy luồng đặt hàng và concurrency chạy thật.
*Mặc định: **(b)**. Đắt hơn 5 pd nhưng bảo đảm demo PRN232 đứng được một mình.*

### 🟡 OQ-32. Nhân viên quét QR trên web bằng webcam được không?
**Vì sao quan trọng:** nếu chọn OQ-31(a) hoặc (b) thì lúc demo PRN232 phải có cách quét
mã mà không cần app Flutter. Thư viện JS quét QR bằng webcam tốn khoảng 1 pd.
*Mặc định: **có**, dùng thư viện JS. Kèm ô nhập mã thủ công làm phương án dự phòng khi
demo — webcam trong phòng máy rất hay trục trặc.*

---

## J. Kỹ thuật & dữ liệu

### 🔴 OQ-33. Múi giờ lưu thế nào?
Lưu UTC rồi đổi ra giờ Việt Nam lúc hiển thị, hay lưu thẳng giờ Việt Nam?
**Vì sao quan trọng:** hệ thống này **toàn thời gian** — 4 mốc mỗi đợt bán, hạn thanh
toán, hạn QR, hạn khiếu nại. Sai múi giờ là sai mọi thứ, và rất khó phát hiện vì trên
máy mỗi người lại đúng.
*Mặc định: **lưu UTC** trong DB, API nhận/trả ISO-8601 có offset, client tự đổi. Ghi rõ
vào contract.*

### 🟡 OQ-34. Seed data bao nhiêu là đủ để demo thuyết phục?
*Mặc định: 3 brand, 8 chi nhánh rải ở TP.HCM có toạ độ thật, 25 mẫu hàng, 6 đợt bán (2
đã đóng, 2 đang chạy, 2 lên lịch), 30 khách, ~60 đơn ở đủ mọi trạng thái, ~20 đánh giá.*

### 🟢 OQ-35. Web client có cần đa ngôn ngữ không?
*Mặc định: **không** — chỉ tiếng Việt.*

---

## Tóm tắt: cần trả lời sớm nhất

Mười ba câu 🔴 chặn việc vẽ ERD:

`OQ-01` `OQ-02` `OQ-03` `OQ-05` `OQ-06` `OQ-08` `OQ-11` `OQ-12` `OQ-16` `OQ-21`
`OQ-25` `OQ-28` `OQ-31` `OQ-33`

Trong đó **`OQ-31` (web client phục vụ ai)** có ảnh hưởng lớn nhất tới tiến độ — chênh
lệch tới 5 person-day, trên một ngân sách chỉ còn dư 2.5.
