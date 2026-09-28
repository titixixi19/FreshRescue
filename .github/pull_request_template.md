## Làm gì


## Liên quan
<!-- Bắt buộc. Mã từ docs/01-REQUIREMENTS.md -->
FR-  BR-  NFR-

## Đã kiểm thử thế nào
<!-- Liệt kê thao tác thật đã chạy, không viết "đã test" -->
- [ ]
- [ ]

## Cần lưu ý khi review


## Checklist
- [ ] `dotnet build` sạch, không có warning mới
- [ ] `dotnet format --verify-no-changes` không báo gì
- [ ] Không còn `Console.WriteLine` gỡ lỗi, không còn code bị comment
- [ ] Đã `git pull --rebase origin dev`
- [ ] Không tự tạo migration (nếu có đổi schema → đã báo Tùng)
- [ ] Nếu đổi endpoint/DTO → đã cập nhật `docs/03-API-CONTRACT.md`
