# FreshRescue.LoadTest — load test chống over-selling (`NFR-01`)

Công cụ console nhỏ cho `T6-TUNG-01`: N khách bấm mua **cùng lúc** một offer chỉ còn Q suất,
rồi đọc thẳng DB để kiểm tra đúng Q đơn thành công và tồn kho không âm. Chạy hai lần với
`Ordering:LockingStrategy` khác nhau để lấy số liệu so sánh cho báo cáo (`02-DOMAIN-MODEL.md` §6).

## Cách chạy

1. Bật API (không cần QrService, bài đo dừng ở bước đặt hàng, chưa thanh toán):

   ```bash
   dotnet run --project src/FreshRescue.API --launch-profile https
   ```

   Muốn đo chế độ lạc quan thì đặt biến môi trường trước khi bật API:

   ```bash
   Ordering__LockingStrategy=Optimistic dotnet run --project src/FreshRescue.API --launch-profile https
   ```

2. Chạy bài đo:

   ```bash
   dotnet run --project tools/FreshRescue.LoadTest -- --label Pessimistic --users 50 --stock 10 --rounds 5
   ```

   | Tuỳ chọn | Mặc định | Ý nghĩa |
   | --- | --- | --- |
   | `--api` | `https://localhost:7001` | Địa chỉ API |
   | `--connection` | LocalDB `FreshRescue` | Chuỗi kết nối để dựng dữ liệu và đọc lại tồn kho |
   | `--users` | 50 | Số khách bấm mua cùng lúc |
   | `--stock` | 10 | Số suất còn lại của offer |
   | `--rounds` | 5 | Số lượt đo |
   | `--warmup` | 1 | Số lượt làm nóng, không tính vào kết quả |
   | `--label` | | Nhãn in ra bảng kết quả |

Công cụ tự tạo 50 tài khoản `load-NNN@freshrescue.test`, một thương hiệu / chi nhánh / đợt bán /
offer với ID cố định tiền tố `10AD7E57-…`, và xoá đơn của lượt trước trước mỗi lượt. Chạy lại
bao nhiêu lần cũng không sinh rác. Mã thoát `2` nếu phát hiện bán lố.

## Kết quả đo ngày 15/10/2026

Cùng một máy: API, công cụ đo và SQL Server LocalDB chạy chung. Mỗi kịch bản 5 lượt, sau 1 lượt
làm nóng. Độ trễ tính từ lúc gửi request tới lúc nhận đủ phản hồi.

### 50 khách tranh 10 suất (kịch bản `NFR-01`)

| Chỉ số | Pessimistic | Optimistic |
| --- | --- | --- |
| Đơn thành công mỗi lượt | **10/10, cả 5 lượt** | 10/10, cả 5 lượt |
| Bán lố | không | không |
| 409 `OFFER_SOLD_OUT` | 200 | 161 |
| 409 `CONCURRENCY_CONFLICT` | 0 | 39 |
| Độ trễ p50 / p95 (ms) | **140 / 235** | 655 / 857 |
| Độ trễ đơn thành công p50 (ms) | **89** | 538 |
| Xử lý hết 50 request (ms) | **194** | 779 |
| Số lần thử lại trong log API | 0 | 552 (44 đơn bỏ cuộc sau 3 lần) |

Optimistic vẫn bán hết 10 suất, nhưng chỉ nhờ thử lại 3 lần có chờ ngẫu nhiên 20–300 ms, nên
chậm hơn **khoảng 4 lần** và tốn 552 lần đọc lại offer.

### 50 khách tranh 30 suất (nhiều người cùng được mua)

| Chỉ số | Pessimistic | Optimistic |
| --- | --- | --- |
| Đơn thành công mỗi lượt | **30/30, cả 5 lượt** | **9–24/30**, trung bình 15,2 — không lượt nào bán hết |
| Bán lố | không | không |
| 409 `CONCURRENCY_CONFLICT` | 0 | 174 (còn hàng vẫn bị từ chối) |
| Độ trễ p50 / p95 (ms) | 336 / 606 | 608 / 753 |
| Xử lý hết 50 request (ms) | 486 | 680 |

Đây là chỗ hai chế độ khác nhau rõ nhất: càng nhiều người **được** mua thì càng nhiều lần ghi
`RowVersion` đè nhau, và sau 3 lần thử lại optimistic trả `CONCURRENCY_CONFLICT` dù kho còn
tới 21 suất. Pessimistic xếp hàng chờ khoá nên bán đúng 30/30.

### 100 khách tranh 10 suất

| Chỉ số | Pessimistic | Optimistic |
| --- | --- | --- |
| Đơn thành công mỗi lượt | **10/10, cả 5 lượt** | 9–10/10 (1 lượt bán 9/10) |
| Bán lố | không | không |
| 409 `CONCURRENCY_CONFLICT` | 0 | 134 |
| Lỗi 500 | 0 | 16 (LocalDB rớt kết nối named pipe khi mở 100 kết nối cùng lúc, không phải lỗi logic) |
| Độ trễ p50 / p95 (ms) | **224 / 331** | 716 / 1308 |
| Xử lý hết 100 request (ms) | **305** | 855 |

### Kết luận

- **Cả hai chế độ đều không bán lố** trong mọi lượt: ràng buộc `CHECK (RemainingQuantity >= 0)` và
  `RowVersion` / `UPDLOCK` đều giữ đúng tồn kho.
- **Pessimistic** (mặc định) đạt đúng Q/Q ở 15/15 lượt, độ trễ thấp hơn 2–4 lần, không có đơn nào bị
  từ chối khi còn hàng. Chọn làm mặc định là đúng.
- **Optimistic** nhanh khi ít tranh chấp nhưng khi đông người cùng mua được thì mất đơn (`CONCURRENCY_CONFLICT`
  dù còn hàng), đúng như nhận định ở `02-DOMAIN-MODEL.md` §6. Giữ lại để so sánh trong báo cáo.
