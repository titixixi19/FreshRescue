# FreshRescue — Phân tích yêu cầu

> Nguồn: `../PRN232.md` · Kế hoạch: `00-PLAN.md`
> Cập nhật: 2026-09-29 — thêm Import/Export (`FR-B07`, `FR-B08`, `FR-A07`) và `NFR-09` theo yêu cầu báo cáo cuối kỳ (`08-REPORT-REQUIREMENTS.md`)
>
> Tài liệu này là **cơ sở chung để cả nhóm hiểu giống nhau** trước khi vẽ ERD.
> Mọi mục đều có mã (FR/BR/NFR) để tham chiếu trong contract, code và báo cáo.

---

## 1. Tuyên bố bài toán

**Vấn đề:** Cuối ngày, cửa hàng F&B còn thực phẩm **vẫn đạt chuẩn an toàn** nhưng hết
hạn bán trong ngày. Bỏ đi thì mất tiền và tạo rác hữu cơ.

**Bên kia của thị trường:** sinh viên và người trẻ muốn mua đồ ăn tốt với giá rẻ, và
sẵn sàng đánh đổi: **tự đến lấy**, **trong khung giờ hẹp**, và **không được chọn món**
(với túi mù).

**FreshRescue** là sàn khớp hai nhu cầu đó.

Ba đánh đổi trên chính là thứ tạo ra mô hình kinh doanh — và cũng là thứ tạo ra
độ khó kỹ thuật của đồ án:

| Đánh đổi nghiệp vụ | Hệ quả kỹ thuật |
| --- | --- |
| Tự đến lấy | Không có module giao hàng; cần GPS + khoảng cách; mọi dòng trong 1 đơn phải cùng 1 cửa hàng |
| Khung giờ hẹp | Vòng đời có thời hạn ở khắp nơi → **BackgroundService**; đơn tự hết hạn |
| Số lượng ít, xả cùng lúc | Tranh chấp mua hàng → **concurrency control**, đây là NFR số 1 |
| Đối soát tại quầy trong vài giây | **QR + redeem idempotent**, phải đọc được cả khi mất mạng |

---

## 2. Tác nhân (Actors)

| Actor | Mô tả | Mục tiêu chính |
| --- | --- | --- |
| **Guest** | Khách vãng lai chưa đăng nhập | Xem được cửa hàng và offer để quyết định có đăng ký không |
| **Customer** | Người tiêu dùng | Mua được đồ ngon giá rẻ gần mình, lấy hàng nhanh gọn |
| **Store Staff** | Nhân viên đứng quầy một chi nhánh | Xả hết hàng dư cuối ca; trả hàng đúng người trong vài giây |
| **Brand Manager** | Chủ thương hiệu / quản lý chuỗi | Chuẩn hoá mặt hàng toàn chuỗi, giám sát hiệu quả từng chi nhánh |
| **System Admin** | Quản trị sàn | Duyệt thương hiệu, xử lý khiếu nại, giữ chất lượng nền tảng |
| **System** | Tiến trình nền | Đóng/mở đợt bán, dọn đơn quá hạn, gửi nhắc nhở |

**Quan hệ:** `Brand 1—n Store`, `Store 1—n Staff`, `Brand 1—1 Brand Manager`.

---

## 3. Từ điển nghiệp vụ

Cả nhóm phải dùng đúng những từ này, trong cả code lẫn báo cáo.

| Thuật ngữ | Tiếng Việt | Nghĩa chính xác |
| --- | --- | --- |
| **Brand** | Thương hiệu | Chuỗi cửa hàng. Đơn vị được Admin duyệt |
| **Store** | Chi nhánh | Một địa điểm vật lý, có toạ độ. Nơi khách tới lấy hàng |
| **OfferTemplate** | Mẫu mặt hàng | **Brand Manager** tạo. Chuẩn hoá tên, ảnh, mô tả, danh mục, giá gốc, khung giá giải cứu cho **toàn chuỗi**. Không có số lượng, không bán trực tiếp |
| **SaleBatch** | Đợt bán | Một lần xả hàng của **một** chi nhánh trong **một** ngày. **Store Staff** tạo cuối ca. Có 4 mốc thời gian (§4) |
| **Offer** | Mặt hàng đang bán | Một `OfferTemplate` được đưa vào một `SaleBatch` kèm **số lượng thực tế** và **giá chốt**. Đây là thứ khách thật sự mua |
| **SurpriseBag** | Túi mù | Khách không biết trước món cụ thể, chỉ biết **danh mục**. Giảm 65–75% |
| **ClearanceItem** | Món lẻ xả kho | Có tên + ảnh cụ thể. Giảm 40–50% |
| **Order** | Đơn hàng | Một lần mua của một khách, tại **một** chi nhánh, trong **một** đợt bán |
| **Reservation** | Giữ chỗ | Việc trừ tồn kho ngay khi tạo đơn, trước khi thanh toán |
| **Pickup Window** | Khung giờ nhận | `[PickupStartTime, PickupEndTime]` — khoảng thời gian khách được tới lấy |
| **Redemption** | Đối soát | Hành động nhân viên quét QR để xác nhận đã trao hàng |
| **No-show** | Bỏ lấy | Khách đã trả tiền nhưng không tới lấy trong khung giờ |
| **Dispute** | Khiếu nại | Phản ánh của khách sau khi đã nhận hàng (đồ hỏng, sai phân loại) |

### 3.1. Vì sao tách `OfferTemplate` khỏi `Offer` *(chốt Q-01)*

Hai mối quan tâm khác nhau, hai người khác nhau, hai nhịp thời gian khác nhau:

| | `OfferTemplate` | `Offer` (trong `SaleBatch`) |
| --- | --- | --- |
| Ai tạo | Brand Manager | Store Staff (Brand Manager tạo thay được) |
| Khi nào | Một lần, dùng lại nhiều tháng | Mỗi ngày, cuối ca |
| Trả lời câu hỏi | "Chuỗi ta bán những loại gì, hình ảnh và khung giá ra sao?" | "Tối nay chi nhánh này còn dư đúng mấy suất?" |
| Có số lượng không | **Không** | **Có** — và đây là chỗ xảy ra tranh chấp |

Lý do nghiệp vụ: chỉ nhân viên đứng quầy lúc cuối ca mới biết chính xác còn dư bao
nhiêu. Nhưng để mỗi nhân viên tự nhập tên và ảnh mỗi tối thì dữ liệu toàn chuỗi sẽ
loạn. Tách ra giải quyết cả hai.

Lý do kỹ thuật: `RowVersion` và `RemainingQuantity` chỉ nằm ở `Offer`. `OfferTemplate`
là dữ liệu tĩnh, đọc nhiều ghi ít — không dính gì tới bài toán concurrency.

---

## 4. Bốn mốc thời gian của một đợt bán

Đây là trái tim của hệ thống. Hiểu sai chỗ này là sai toàn bộ.

```
  SaleStartTime      SaleEndTime
      │                  │
      ▼                  ▼
──────┼──────────────────┼──────────────────────────────►  thời gian
      │   ĐƯỢC ĐẶT HÀNG  │
              │                  │
              ▼                  ▼
        PickupStartTime    PickupEndTime
              │    ĐƯỢC LẤY      │
              │      HÀNG        │
```

Ví dụ thực tế: bán **19:30 – 21:00**, nhận hàng **20:30 – 21:30**.

> **Cả 4 trường đều là `DateTime`, không phải `TimeOnly`.** Một đợt bán gắn với một
> ngày cụ thể. Đặt tên có chữ "Time" nhưng đừng mô hình hoá thành giờ trong ngày.

Hai khoảng này **chồng lấn nhau**, không nối tiếp. Từ 20:30 đến 21:00, vừa có người
đang mua vừa có người đang tới lấy — đó là tình huống thật ở cửa hàng, và cũng là lý do
bài toán tranh chấp tồn kho trở nên thú vị.

**Ràng buộc:** `SaleStartTime < SaleEndTime ≤ PickupEndTime`, `PickupStartTime < PickupEndTime`,
và `SaleStartTime ≤ PickupStartTime` (`BR-15`).

---

## 5. Luồng nghiệp vụ chính (happy path)

### 5.1. Chuẩn bị (không diễn ra hằng ngày)

Brand Manager tạo sẵn các `OfferTemplate`: *"Túi mù bánh mặn"* (giá gốc 100k, giá giải
cứu 35k), *"Hộp cơm cá hồi"* (giảm 50%)… kèm ảnh và mô tả chuẩn của chuỗi.

### 5.2. Bốn bước tạo đợt bán *(chốt Q-03)*

| Bước | Việc | Ai |
| --- | --- | --- |
| 1 | **Chọn mẫu hàng** — nhặt từ danh sách `OfferTemplate` của brand | Staff |
| 2 | **Nhập số lượng thực tế** — kiểm đếm quầy: túi ngọt 4 suất, túi mặn 2 suất | Staff |
| 3 | **Cài khung thời gian** — 4 mốc ở §4 | Staff |
| 4 | **Kích hoạt** — đợt chuyển `Active`, bắn thông báo, hiện trên feed/bản đồ của khách gần đó | Staff |

### 5.3. Vòng đời một đơn hàng

```
 Staff                    System                  Customer                 Staff
   │                        │                        │                      │
   │ 4 bước tạo đợt bán     │                        │                      │
   ├───────────────────────►│                        │                      │
   │                        │ tới SaleStartTime      │                      │
   │                        │ → mở đợt, bắn thông báo│                      │
   │                        ├───────────────────────►│                      │
   │                        │                        │ chọn offer, đặt hàng │
   │                        │◄───────────────────────┤                      │
   │                        │ GIỮ CHỖ (trừ tồn ngay) │                      │
   │                        │ → đơn PendingPayment   │                      │
   │                        ├───────────────────────►│                      │
   │                        │                        │ thanh toán (≤10 phút)│
   │                        │◄───────────────────────┤                      │
   │                        │ → Paid, CẤP VÉ QR      │                      │
   │                        ├───────────────────────►│                      │
   │                        │ nhắc trước 30'         │                      │
   │                        ├───────────────────────►│                      │
   │                        │                        │ tới quầy, chìa QR    │
   │                        │                        ├─────────────────────►│
   │                        │◄──────────────────────────  quét & đối soát   │
   │                        │ → Redeemed             │                      │
```

**Các nhánh rẽ:**

- Không thanh toán trong 10 phút → `Cancelled` (bởi *System*) + **trả tồn kho về** (`FR-X03`)
- Khách chủ động hủy trong thời hạn cho phép → `Cancelled` (bởi *Customer*) + hoàn 100% + trả tồn kho
- Cửa hàng gặp sự cố, hủy đơn đã thanh toán → `Cancelled` (bởi *Store*) + hoàn 100%
- Tới `SaleEndTime` chưa bán hết → đợt đóng, offer còn lại bỏ
- Đã trả tiền nhưng không tới lấy → `NoShow` sau `PickupEndTime`, **không hoàn tiền**
- Đã nhận hàng nhưng đồ hỏng / sai loại → khách khiếu nại trong 2 giờ, Admin duyệt rồi hoàn tiền
- Brand Manager huỷ cả đợt bán → mọi đơn `Paid` được hoàn 100%, mọi đơn `PendingPayment` bị
  huỷ và trả tồn kho (`BR-23`)

---

## 6. Yêu cầu chức năng

### 6.1. Guest / Customer

| Mã | Yêu cầu | Ưu tiên |
| --- | --- | --- |
| FR-C01 | Đăng ký, đăng nhập, làm mới token | Bắt buộc |
| FR-C02 | Xem cửa hàng gần mình, sắp xếp theo khoảng cách (Haversine) | Bắt buộc |
| FR-C03 | Lọc/tìm offer: loại, danh mục, khoảng giá, khoảng cách, còn hàng | Bắt buộc |
| FR-C04 | Xem chi tiết cửa hàng + đợt bán đang mở + đánh giá | Bắt buộc |
| FR-C05 | Đặt hàng: nhiều offer, cùng một chi nhánh | Bắt buộc |
| FR-C06 | Thanh toán trong thời hạn giữ chỗ (mock) | Bắt buộc |
| FR-C07 | Nhận vé QR sau khi thanh toán thành công | Bắt buộc |
| ~~FR-C08~~ | ~~Mở vé QR kể cả khi mất mạng~~ — **đã cắt 28/09** cùng với PRM393, xem `00-PLAN.md` §0 | ❌ |
| FR-C09 | Xem lịch sử đơn + trạng thái | Bắt buộc |
| FR-C10 | Hủy đơn theo `BR-13`, được hoàn 100% | Bắt buộc |
| FR-C11 | Đánh giá đơn đã nhận (sao + bình luận + ảnh chụp) | Nên có |
| FR-C12 | Đánh dấu cửa hàng yêu thích | Nên có |
| FR-C13 | Nhận thông báo **trong ứng dụng** (bảng `Notification`): đợt bán mới, nhắc trước giờ đóng 30' | Nên có |
| ~~FR-C14~~ | ~~Đăng ký device token để nhận push~~ — **đã cắt 28/09**, không có app di động | ❌ |
| FR-C15 | **Gửi khiếu nại** kèm ảnh trong 2 giờ sau khi nhận hàng | Nên có |

### 6.2. Store Staff

| Mã | Yêu cầu | Ưu tiên |
| --- | --- | --- |
| FR-S01 | Đăng nhập; token mang sẵn `store_id` | Bắt buộc |
| FR-S02 | **Tạo đợt bán theo 4 bước §5.2** cho chi nhánh của mình | Bắt buộc |
| FR-S03 | Điều chỉnh số lượng còn lại, đóng đợt sớm | Bắt buộc |
| FR-S04 | Xem danh sách đơn phải trả trong đợt đang diễn ra | Bắt buộc |
| FR-S05 | **Quét QR để đối soát** | Bắt buộc |
| FR-S06 | Nhập mã đơn thủ công khi khách hết pin / hỏng camera | Nên có |
| FR-S07 | **Hủy đơn đã thanh toán do sự cố** (hết hàng, đồ hỏng) → hoàn 100% + thông báo | Nên có |

### 6.3. Brand Manager

| Mã | Yêu cầu | Ưu tiên |
| --- | --- | --- |
| FR-B01 | Quản lý hồ sơ thương hiệu | Bắt buộc |
| FR-B02 | CRUD chi nhánh (kèm toạ độ) | Bắt buộc |
| FR-B03 | Tạo tài khoản nhân viên, gán vào chi nhánh | Bắt buộc |
| FR-B04 | **CRUD `OfferTemplate`** — chuẩn hoá mặt hàng toàn chuỗi | Bắt buộc |
| FR-B05 | Xem / tạo / sửa đợt bán của **mọi chi nhánh thuộc brand mình** | Bắt buộc |
| FR-B06 | Thống kê theo chi nhánh — **rút gọn còn 2 biểu đồ**: doanh thu và số suất đã cứu. Định nghĩa số liệu ở `BR-25` | Nên có |
| FR-B07 | **Import `OfferTemplate` từ file Excel**: tải file mẫu, điền, tải lên. Quy tắc ở `BR-26` | Bắt buộc *(yêu cầu báo cáo)* |
| FR-B08 | **Export đơn hàng + thống kê** của thương hiệu ra Excel, lọc theo khoảng ngày. Quy tắc ở `BR-27` | Bắt buộc *(yêu cầu báo cáo)* |

### 6.4. System Admin

| Mã | Yêu cầu | Ưu tiên |
| --- | --- | --- |
| FR-A01 | Duyệt / tạm khoá thương hiệu | Bắt buộc |
| FR-A02 | Quản lý người dùng và vai trò | Bắt buộc |
| FR-A03 | Quản lý danh mục (Bánh ngọt, Đồ mặn, Đồ chay…) | Bắt buộc |
| FR-A04 | **Duyệt / từ chối khiếu nại**; duyệt thì kích hoạt hoàn tiền | Nên có |
| FR-A07 | **Export đơn hàng toàn sàn** ra Excel, lọc theo khoảng ngày (`BR-27`) | Bắt buộc *(yêu cầu báo cáo)* |
| ~~FR-A05~~ | ~~Thống kê toàn sàn~~ — **đã cắt 17/09**, xem `00-PLAN.md` §1.2 | ❌ |
| ~~FR-A06~~ | ~~Nhật ký đối soát~~ — **đã cắt**; `NFR-05` vẫn được thoả bằng 2 cột trên `QrTicket` | ❌ |

### 6.5. System — tiến trình nền

| Mã | Yêu cầu | Chạy khi |
| --- | --- | --- |
| FR-X01 | Chuyển đợt bán `Scheduled` → `Active` | Tới `SaleStartTime` |
| FR-X02 | Chuyển đợt bán `Active` → `Closed` | Tới `PickupEndTime` |
| FR-X03 | Hủy đơn quá hạn thanh toán và **trả tồn kho về** | Mỗi phút |
| FR-X04 | Đánh dấu `NoShow` | Sau `PickupEndTime` |
| FR-X05 | Nhắc khách trước giờ đóng nhận hàng 30' | Định kỳ |
| FR-X06 | Đẩy realtime số lượng còn lại (SignalR) | Khi tồn kho đổi |

> **Không có sự kiện đổi trạng thái tại `SaleEndTime`.** Đợt bán vẫn ở `Active` trong
> khoảng `[SaleEndTime, PickupEndTime]` — lúc đó không ai đặt được nữa nhưng khách cũ
> vẫn tới lấy hàng. Việc "hết giờ bán" do `BR-02` kiểm theo thời gian quyết định, không
> phải do trạng thái. Trạng thái chỉ đổi ở 2 mốc: `SaleStartTime` và `PickupEndTime`.
>
> `FR-X04` (đánh dấu `NoShow`) cũng chạy tại `PickupEndTime` — **phải chạy trước** khi
> đóng đợt, nếu không sẽ không còn đơn nào ở trạng thái hợp lệ để đánh dấu.

---

## 7. Quy tắc nghiệp vụ

Đây là phần phải đưa vào **service layer**, không phải controller, và là phần đáng viết
unit test nhất.

| Mã | Quy tắc |
| --- | --- |
| **BR-01** | `Offer.RemainingQuantity` **không bao giờ âm**, kể cả dưới tải đồng thời cao |
| **BR-02** | Chỉ đặt được khi đợt bán đang `Active` và hiện tại nằm trong `[SaleStartTime, SaleEndTime]` |
| **BR-03** | Mọi dòng trong một đơn phải thuộc **cùng một chi nhánh và cùng một đợt bán**. Lý do: mỗi đơn chỉ sinh **một** mã QR, quét tại **một** quầy |
| **BR-04** | Tạo đơn là **trừ tồn kho ngay** (giữ chỗ), không đợi thanh toán |
| **BR-05** | Hạn thanh toán = `min(thời điểm tạo + 10 phút, **PickupEndTime**)`. Quá hạn → `Cancelled`, hoàn tồn kho. *Cố tình **không** dùng `SaleEndTime`: điều kiện thực sự cần là khách trả tiền kịp để còn tới lấy, không phải trả trước khi hết giờ bán. Nếu chặn theo `SaleEndTime` thì người đặt lúc 20:59:30 của đợt đóng 21:00 chỉ có 30 giây để trả tiền* |
| **BR-06** | Giới hạn mua: mỗi khách tối đa **3 suất / mỗi offer / mỗi đợt bán**. Cộng dồn `PendingPayment` + `Paid` + **`Redeemed`**, **không** tính theo từng đơn. *Phải đếm cả `Redeemed`: vì khung bán và khung nhận chồng nhau (§4), khách có thể đặt 3 suất lúc 20:35, tới lấy lúc 20:40, rồi đặt tiếp 3 suất lúc 20:45 — hạn mức thành 6* |
| **BR-07** | **Biên độ giảm giá hai tầng.** Tầng sàn (cứng, theo loại): `SurpriseBag` 65–75%, `ClearanceItem` 40–50%. Tầng brand: `OfferTemplate` khai báo biên độ riêng, được **siết chặt hơn** nhưng **không được nới rộng** ra ngoài tầng sàn. `Offer.ActualPrice` do Staff nhập phải thoả **cả hai** tầng, sai → `400` |
| **BR-08** | Chỉ đối soát được trong `[PickupStartTime − 5 phút, PickupEndTime]` |
| **BR-09** | Đối soát **idempotent**: quét lần thứ hai trả `409`, không trừ hàng lần nữa |
| **BR-10** | Staff chỉ đối soát / hủy được đơn thuộc **chi nhánh của mình**. **Brand Manager cũng được đối soát và hủy đơn**, phạm vi là mọi chi nhánh thuộc thương hiệu mình — chủ chuỗi đứng quầy là chuyện bình thường |
| **BR-11** | Brand Manager chỉ thao tác được trên chi nhánh và `OfferTemplate` thuộc **thương hiệu của mình** |
| **BR-12** | Chỉ đánh giá được đơn ở trạng thái `Redeemed`; mỗi đơn đúng **một** đánh giá |
| **BR-13** | Khách hủy được khi: đơn `PendingPayment` (chưa trả tiền, chỉ trả tồn kho), hoặc đơn `Paid` và `now ≤` **`min(PaidAt + 15 phút, PickupStartTime − 5 phút)`** → hoàn 100%. *Mốc `− 5 phút` cố ý khớp với `BR-08`, để cửa sổ hủy và cửa sổ đối soát **không bao giờ giao nhau**. Nếu để hai cửa sổ chồng lên, sẽ có lúc một đơn `Paid` vừa hủy được vừa quét được — nhân viên trao bánh lúc 20:26, khách bấm hủy lúc 20:27 và vẫn được hoàn tiền* |
| **BR-14** | Thương hiệu phải ở trạng thái `Approved` mới được mở đợt bán |
| **BR-15** | `SaleStartTime < SaleEndTime ≤ PickupEndTime`, `PickupStartTime < PickupEndTime`, và **`SaleStartTime ≤ PickupStartTime`**. *Thiếu mệnh đề cuối thì vẫn cấu hình được đợt bán cho lấy hàng trước cả khi mở bán* |
| **BR-16** | Cửa hàng hủy được đơn `Paid` chưa `Redeemed`, **bắt buộc nhập lý do** → hoàn 100% + thông báo cho khách |
| **BR-17** | Khiếu nại chỉ mở được với đơn `Redeemed`, trong **2 giờ** kể từ `RedeemedAt`, mỗi đơn đúng **một** khiếu nại |
| **BR-18** | `Offer` **chụp lại (snapshot)** tên, ảnh, giá từ `OfferTemplate` lúc tạo. Brand sửa template sau đó **không** làm đổi các đợt bán đã tạo |
| **BR-19** | `NoShow` **không hoàn tiền** — cửa hàng đã giữ đồ và không bán tiếp được |
| **BR-20** | Mỗi đơn lưu tách bạch `TotalAmount` (khách trả) = `PlatformFee` + `StorePayoutAmount`. Tính và **đóng băng lúc tạo đơn**, không tính lại dù tỉ lệ phí của sàn có đổi. Thứ tự tính bắt buộc: `PlatformFee = ROUND(TotalAmount × 0.10, 0)` rồi `StorePayoutAmount = TotalAmount − PlatformFee`. *Tính riêng 90% cho vế sau sẽ làm tổng lệch khỏi `TotalAmount` ở các số không chia hết — cột tiền là `decimal(18,0)`* |
| **BR-21** | Hoàn tiền là hoàn `TotalAmount` cho khách; sàn **không giữ lại** `PlatformFee`. Ghi một bản ghi `Payment` âm loại `Refund` |
| **BR-22** | Xoá mềm áp dụng cho `Brand`, `Store`, `OfferTemplate`, `Offer`, `Category`. Truy vấn **lịch sử đơn hàng** phải gọi `IgnoreQueryFilters()`, nếu không đơn cũ trỏ tới chi nhánh đã xoá sẽ mất dữ liệu liên kết |
| **BR-23** | Huỷ cả đợt bán (`SaleBatch → Cancelled`) kéo theo: huỷ mọi đơn `PendingPayment` (trả tồn kho) **và** mọi đơn `Paid` (hoàn 100%, `CancelledBy = Store`), dùng lại đúng luồng `BR-16`. **Chỉ Brand Manager** được huỷ đợt, bắt buộc nhập lý do. Đợt đã có đơn `Redeemed` thì **không** huỷ được nữa |
| **BR-24** | **Cấm xoá `Offer` đã có `OrderItem`.** Muốn ngừng bán giữa chừng thì đặt `RemainingQuantity = 0`. *Xoá mềm sẽ bị Global Query Filter giấu đi, khiến đơn cũ trỏ vào khoảng không* |
| **BR-25** | Định nghĩa số liệu cho `FR-B06`: **doanh thu** = `SUM(StorePayoutAmount)` của đơn `Status IN (Redeemed, NoShow)` **và** `RefundStatus = None`; **số suất đã cứu** = `SUM(OrderItem.Quantity)` của đơn `Redeemed`. *Không định nghĩa rõ thì hai biểu đồ sẽ đếm cả đơn đã hoàn tiền* |
| **BR-26** | *(Đề xuất 29/09 — chờ nhóm duyệt)* Import `OfferTemplate` là **tất cả hoặc không**: mỗi dòng được kiểm y như `POST /brand/offer-templates`, kể cả `BR-07`; chỉ cần **một** dòng sai là không nhập dòng nào, trả `400 VALIDATION_FAILED` kèm lỗi theo số dòng. Chỉ nhận `.xlsx` ≤ 5 MB, tối đa 200 dòng. Mẫu nhập vào luôn thuộc `brand_id` trong token (`BR-11`). *Nhập được một nửa thì Brand Manager không biết dòng nào đã vào, dòng nào chưa — sửa file rồi nhập lại sẽ sinh bản trùng* |
| **BR-27** | *(Đề xuất 29/09 — chờ nhóm duyệt)* Export chỉ gồm đơn trong phạm vi quyền: Brand Manager chỉ thấy chi nhánh của mình (`BR-11`), Admin thấy toàn sàn. Truy vấn phải `IgnoreQueryFilters()` (`BR-22`) để đơn của chi nhánh đã xoá vẫn có mặt. Cột thời gian trong file đổi sang **giờ Việt Nam (UTC+7)** và ghi rõ ở tiêu đề cột — file Excel là để người đọc, không phải để máy đọc lại (`OQ-33` chỉ áp cho API JSON) |

### 7.1. Mô hình trạng thái đơn hàng

Giữ tập trạng thái nhỏ, tách "ai hủy" và "đã hoàn tiền chưa" thành trường riêng —
nếu không, mỗi tổ hợp lại đẻ ra một trạng thái mới và state machine sẽ nổ.

```
Status:      PendingPayment ─► Paid ─► Redeemed
                   │             │
                   ▼             ▼
               Cancelled     Cancelled  /  NoShow

CancelledBy:  System (quá hạn) | Customer | Store        ← chỉ có nghĩa khi Cancelled
RefundStatus: None | Refunded                            ← trường riêng, không phải trạng thái
```

> **`Refunded` không được là một giá trị của `Status`.** Một đơn đã `Redeemed` rồi bị
> khiếu nại và được hoàn tiền thì **vẫn là đơn đã trao hàng** — hàng đã ra khỏi quầy
> thật. Ghi đè `Status = Refunded` sẽ làm đơn đó biến mất khỏi thống kê đối soát của
> chi nhánh, và không còn phân biệt được "hoàn tiền vì khách hủy" với "hoàn tiền vì
> khiếu nại sau khi nhận".

---

## 8. Yêu cầu phi chức năng

| Mã | Yêu cầu | Cách kiểm chứng |
| --- | --- | --- |
| **NFR-01** | **Không over-sell** dưới tải đồng thời | Load test: 50 request đồng thời mua 10 túi → đúng 10 thành công |
| NFR-02 | Trả đúng HTTP status code (`200/201/400/401/403/404/409`) | Review + Swagger |
| NFR-03 | JWT + Refresh Token; mật khẩu băm; HTTPS | Review |
| NFR-04 | Vé QR **tự chứa dữ liệu + ký HMAC**; chỉ `QrService` giữ khoá (`OQ-22`). Sửa một ký tự trong token là chữ ký sai. *Bản trước còn yêu cầu mở vé khi offline cho app di động — đã bỏ 28/09* | Sửa token rồi quét → `QR_INVALID_SIGNATURE` |
| NFR-05 | Lưu vết đối soát: ai quét, đơn nào, lúc nào | Bảng redemption |
| NFR-06 | Mọi API danh sách đều phân trang, trả `PagedResponse<T>` | Contract |
| NFR-07 | Swagger đầy đủ mô tả, có Authorize | Mở Swagger |
| NFR-08 | API danh sách phản hồi < 500 ms với seed data | Đo tay |
| NFR-09 | **Mọi bảng có ít nhất 20 bản ghi** sau khi seed *(yêu cầu báo cáo cuối kỳ)*. Ngoại lệ không tránh được: `AspNetRoles` chỉ có 4 role cố định (`02-DOMAIN-MODEL.md` §3.1) | `SELECT COUNT(*)` từng bảng |

---

## 9. Các quyết định đã chốt

| Mã | Câu hỏi | **Quyết định** |
| --- | --- | --- |
| **Q-01** | Ai tạo đợt bán và offer? | **Tách 2 tầng.** Brand Manager tạo `OfferTemplate` (chuẩn hoá toàn chuỗi). Store Staff tạo `SaleBatch` hằng ngày theo 4 bước §5.2. Brand Manager tạo thay được khi cần |
| **Q-02** | Một đơn mua được bao nhiêu offer? | **Nhiều offer, bắt buộc cùng một chi nhánh** (`BR-03`). Giới hạn 3 suất/offer/đợt, **cộng dồn mọi đơn đang sống** (`BR-06`) |
| **Q-03** | Đợt bán tạo thế nào? | **4 bước:** chọn mẫu → nhập số lượng thực tế → cài 4 mốc thời gian → kích hoạt |
| **Q-04** | Túi mù có lưu danh sách món bên trong không? | **Không.** Chỉ danh mục + giá trị ước tính (`OriginalPrice`) |
| **Q-05** | Trả trước hay trả khi lấy hàng? | **Trả trước** |
| **Q-06** | Chính sách hủy & hoàn tiền? | **Bản gọn** (§9.1) |
| **Q-07** | Một nhân viên thuộc nhiều chi nhánh? | **Không** — 1 staff = 1 store |
| **Q-08** | Guest chưa đăng nhập xem được gì? | Xem cửa hàng + offer; **phải đăng nhập mới đặt hàng** |
| **Q-09** | Quét QR trước giờ nhận hàng? | Cho phép sớm **5 phút** (`BR-08`) |
| **Q-10** | Khách no-show có bị phạt uy tín? | **Không** ở phiên bản này — chỉ ghi nhận để thống kê |

### 9.1. Chính sách hủy & hoàn tiền — phạm vi đã chốt

| Tình huống | Xử lý | Làm ở PRN232 |
| --- | --- | --- |
| Khách hủy đúng hạn (`BR-13`) | Hoàn 100% + trả tồn kho | ✅ |
| Quá hạn thanh toán | `Cancelled` bởi System, trả tồn kho | ✅ |
| Khách không tới lấy | `NoShow`, **không hoàn tiền** (`BR-19`) | ✅ |
| Cửa hàng hủy do sự cố (`BR-16`) | Hoàn 100% + thông báo xin lỗi | ✅ |
| Khách khiếu nại sau khi nhận (`BR-17`) | Gửi ảnh + lý do → Admin duyệt → hoàn tiền thủ công | ✅ *bản đơn giản* |
| **Voucher đền bù** | — | ❌ **hoãn** |

**Hai điểm đã điều chỉnh so với đề xuất ban đầu**, có lý do:

1. **Bỏ mệnh đề "hủy được nếu còn ≥ 1 tiếng trước giờ pickup".** Với ví dụ thật của
   chính đợt bán (bán 19:30, pickup 20:30) thì khoảng cách **đúng bằng 1 tiếng** — nghĩa
   là chỉ đơn đặt lúc 19:30:00 mới thoả, đặt lúc 19:31 là hết quyền. Với đợt bán nào có
   khoảng cách ngắn hơn thì mệnh đề này không bao giờ kích hoạt. Gộp còn một quy tắc duy
   nhất ở `BR-13`.
2. **Giới hạn mua tính theo đợt bán, không theo đơn.** Nếu chỉ giới hạn "3 suất mỗi lần
   đặt" thì người gom hàng chỉ cần đặt ba đơn liên tiếp. `BR-06` cộng dồn mọi đơn đang
   sống của khách trong đợt đó.

**Khiếu nại — bản đơn giản nghĩa là:** khách gửi ảnh + lý do; Admin/Brand Manager có một
danh sách chờ, bấm *Duyệt* (kích hoạt hoàn tiền) hoặc *Từ chối* kèm ghi chú. Không có
SLA, không có nhiều vòng trao đổi, không có hàng đợi phân công.

### 9.2. Lớp quyết định thứ hai (OQ-01 → OQ-35, chốt 17/09)

Bối cảnh và lý lẽ đầy đủ ở `04-OPEN-QUESTIONS.md`. Đây là bản rút gọn để tra nhanh.

| Mã | Quyết định |
| --- | --- |
| **OQ-01/02** | Brand **tự đăng ký** trên web → `PendingApproval` → Admin duyệt. Người đăng ký thành `BrandManager` và là `Brand.OwnerUserId`; chỉ đủ quyền thao tác sau khi duyệt |
| **OQ-03** | Một `User` **nhiều role**. JWT mang `Role[]` + `StoreId` (nullable). Khi mua hàng bỏ qua `StoreId`; khi thao tác tại quầy, middleware bắt buộc role `StoreStaff` **và** kiểm phạm vi `StoreId` |
| **OQ-04** | Email + mật khẩu. Reset mật khẩu qua token gửi email. **Không** SMS OTP |
| **OQ-05** | `Store.OpenTime` / `CloseTime` kiểu **`TimeOnly`** *(không dùng `TimeSpan`)*, chỉ để hiển thị. **Không** ràng buộc đợt bán phải nằm trong giờ mở cửa |
| **OQ-06** | Xoá mềm 100% qua interface `ISoftDelete` (`IsDeleted`, `DeletedAt`) + Global Query Filter. Xem `BR-22` về bẫy khi truy vấn lịch sử |
| **OQ-08** | `OfferTemplate` giữ `OriginalPrice` + `SuggestedPrice` + biên độ của brand. `Offer.ActualPrice` do Staff nhập, validate theo `BR-07` **hai tầng** |
| **OQ-09** | Endpoint upload ảnh **dùng chung** cho mặt hàng / đánh giá / khiếu nại. Lưu `wwwroot/uploads/`, DB giữ đường dẫn tương đối |
| **OQ-10** | `OfferTemplate` dùng chung toàn chuỗi, không gắn chi nhánh |
| **OQ-11** | Cho phép **nhiều đợt bán/ngày**. Chặn hai đợt của cùng chi nhánh có `[SaleStartTime, SaleEndTime]` chồng lấn |
| **OQ-12** | `Draft → Scheduled → Active → Closed` (+ `Cancelled`). Xem ghi chú ở §6.5 về việc **không** đổi trạng thái tại `SaleEndTime` |
| **OQ-13** | **Có** lên lịch trước — đây là lý do tồn tại của `FR-X01` |
| **OQ-14** | Sau đơn `Paid` đầu tiên: **chỉ được tăng số lượng**. Cấm hạ số lượng dưới mức đã bán, cấm đổi giá, và **cấm đổi `PickupEndTime`** — xem §9.4. Khi đợt mới chỉ có đơn `PendingPayment` thì vẫn được lùi giờ |
| **OQ-15** | Lưu `UnsoldQuantity` khi đóng đợt, phục vụ số liệu ESG |
| **OQ-16** | **Có phí sàn 10%.** `Order` lưu `TotalAmount` / `PlatformFee` / `StorePayoutAmount` (`BR-20`) |
| **OQ-17** | Cổng thanh toán giả có 2 nút **Thành công** / **Thất bại**. Thất bại hoặc tắt tab → vẫn giữ chỗ đủ 10 phút rồi mới bị `FR-X03` thu hồi |
| **OQ-18** | Được đặt đơn mới khi còn đơn chưa thanh toán; vẫn bị `BR-06` chặn |
| **OQ-19** | Mã đơn `FR-XXXXXXXX` — 8 ký tự hoa, bỏ `O` `0` `I` `1` (32 ký tự × 8 vị trí ≈ 1,1 nghìn tỉ tổ hợp) |
| **OQ-20** | Giá đã gồm mọi khoản. Tiền kiểu `decimal(18,0)` — VND không có phần lẻ |
| **OQ-21** | `Order` ↔ `QrTicket` quan hệ **1–1**. Hai đơn thì quét hai lần |
| **OQ-22** | **Chỉ `QrService` giữ khoá bí mật.** Đây là lý do chính đáng để gRPC tồn tại |
| **OQ-23** | Quét nhầm chi nhánh → `403` kèm tên chi nhánh đúng |
| **OQ-24** | Cửa hàng hết đồ thật → dùng `FR-S07`, bắt buộc nhập lý do |
| **OQ-25** | **Chỉ `SystemAdmin`** duyệt/bác khiếu nại. Brand Manager & Staff chỉ xem và ghi `MerchantNote` để Admin đối chiếu |
| **OQ-26** | Không làm ví ảo. Duyệt hoàn tiền → `RefundStatus = Refunded` + bản ghi `Payment` âm loại `Refund`. **`Status` của đơn giữ nguyên** (xem §7.1) |
| **OQ-27** | Khiếu nại bị từ chối **không** mở lại được |
| **OQ-28** | Đánh giá gắn vào `Order`, quy về `Store`. `Store.RatingAverage` `decimal(2,1)` + `RatingCount` `int`, cập nhật khi có đánh giá mới |
| **OQ-29** | Thông báo đợt bán mới **chỉ gửi cho người đã yêu thích** chi nhánh |
| **OQ-31** | Web Client = **toàn bộ nghiệp vụ quản trị** (Admin/Brand/Staff) + **1 storefront + Quick Checkout** cho khách. Bản đồ, vé offline, push **không làm** — không có app di động (28/09) |
| **OQ-32** | Quét QR trên web bằng **HTML5-QRCode qua webcam**, luôn kèm ô nhập mã 8 ký tự làm dự phòng |
| **OQ-33** | **Lưu UTC** trong DB. API nhận/trả ISO-8601 có offset. Client tự đổi sang UTC+7 khi hiển thị |
| **OQ-34/35** | Seed **ít nhất 20 bản ghi cho mọi bảng** (`NFR-09`, đổi 29/09 — mặc định cũ ở `04-OPEN-QUESTIONS.md` không còn đủ). Web chỉ tiếng Việt |

### 9.3. Hai điều chỉnh so với bản chốt gốc — và vì sao

1. **Biên độ giảm giá (`OQ-08` ↔ `BR-07`).** Bản chốt ghi *"tối đa 80%, tối thiểu 50%"*,
   nhưng `ClearanceItem` theo đề bài gốc chỉ giảm **40–50%** — rơi ra ngoài khoảng đó.
   Giải pháp: **hai tầng** — sàn quy định cứng theo loại, brand chỉ được siết chặt hơn.

2. **Quick Checkout phải hiện mã QR (`OQ-31`).** Nếu modal dừng ở bước thanh toán thì
   không có mã nào để nhân viên quét — Web Client là giao diện demo duy nhất, nên sẽ
   mất trắng phần demo `FR-S05`. Luồng web tối thiểu phải là:
   **chọn offer → đặt → thanh toán mock → hiện QR → nhân viên quét.** Thêm ~0.5 pd.

### 9.4. Vì sao cấm đổi `PickupEndTime` sau đơn `Paid` đầu tiên

`OQ-14` ban đầu cho phép Staff lùi `PickupEndTime` về sau. Điều đó **mâu thuẫn trực
tiếp** với thiết kế vé QR.

Token QR **tự chứa** `exp` và được ký HMAC (`NFR-04`, `02-DOMAIN-MODEL.md` §6.5) — hạn
dùng nằm *bên trong chuỗi đã ký*, không phải trong một cột của CSDL. Lùi giờ trong CSDL
không làm token cũ dài thêm, và cũng không sửa được token đã nằm sẵn trên máy khách
(trang QR đang mở, ảnh chụp màn hình).

Hậu quả nếu cho phép: cửa hàng gia hạn tới 22:00, khách tới lúc 21:40, vé vẫn báo hết hạn.

Ba cách xử lý và lý do chọn:

| Cách | Đánh giá |
| --- | --- |
| Phát hành lại toàn bộ vé khi gia hạn | Khách đang giữ vé cũ (trang đã mở, ảnh chụp) không biết có vé mới — vẫn hỏng, lại tốn công |
| Cho `exp` dư ra một khoảng ân hạn | Nới lỏng bảo mật của vé để đổi lấy một tính năng hiếm dùng |
| **Cấm đổi sau đơn `Paid` đầu tiên** ✅ | Không phát sinh mã, không nới bảo mật. Trước khi có đơn `Paid` thì chưa có vé nào, vẫn sửa thoải mái |

---

## 10. Ngoài phạm vi (tuyên bố rõ để khỏi tranh cãi)

- Giao hàng tận nơi — mô hình là **self-pickup**
- Cổng thanh toán thật (chỉ mock; VNPay chỉ làm nếu Tuần 7 còn dư)
- **Voucher / mã giảm giá / đền bù** — hoãn sang backlog
- **Module quyết toán (Payout) thật** — chỉ lưu 3 cột tiền ở `Order` (`BR-20`) và làm
  báo cáo tổng hợp cho Admin. **Không** làm kỳ quyết toán, đối soát chuyển khoản, trạng
  thái đã/chưa chi trả cho từng chi nhánh — đó là một phân hệ riêng, ~4 pd
- Chat giữa khách và cửa hàng
- Chương trình tích điểm, hạng thành viên
- Điểm uy tín khách hàng / phạt no-show
- Đa ngôn ngữ, đa tiền tệ
- Quản lý kho nguyên liệu của cửa hàng

---

## 11. Truy vết yêu cầu → rubric PRN232

Để báo cáo chỉ rõ từng chương của môn học được thể hiện ở đâu.

| Chương | Thể hiện ở |
| --- | --- |
| Ch.02 Models in Web API | Entities + DTO + AutoMapper |
| Ch.03 Media Formatters & Content Negotiation | Hỗ trợ JSON + XML |
| Ch.04 **OData** | `FR-C02`, `FR-C03` — lọc/sắp xếp cửa hàng & offer |
| Ch.05 Binding, Validation, Routing | FluentValidation cho `BR-07`, `BR-15` |
| Ch.06 **Security** | `FR-C01`, `BR-10`, `BR-11` — JWT + custom claim + policy |
| Ch.07 JavaScript Clients | Web Client: fetch/AJAX + SignalR JS |
| Ch.09 **gRPC** | Dịch vụ cấp & xác thực QR (`FR-C07`, `FR-S05`) |
| Ch.10 Microservices | QrService tách tiến trình riêng |
| *Ngoài chương trình* | Concurrency (`NFR-01`), BackgroundService (`FR-X01…X06`), SignalR (`FR-X06`) |
| *Yêu cầu báo cáo cuối kỳ* | Import/Export (`FR-B07`, `FR-B08`, `FR-A07`) · search & sort (OData `$filter`, `$orderby=a,b desc`) · đăng ký / đăng nhập / reset password (`FR-C01`, `OQ-04`) · ≥ 20 bản ghi mỗi bảng (`NFR-09`) |
