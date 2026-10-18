# Kết quả kiểm tra gói UI — 17/10/2026

## Đã chạy

- Chromium153 thực qua Playwright: mở file offline, không JavaScript pageerror trong lượt capture.
- 12 món; thêm đúng một lần; card không phát bụi sao.
- Mở thanh toán không phát bụi sao; xác nhận bị khóa sau lần kích hoạt; hạt tự dọn hết sau lifetime.
- Ảnh viewport1366×768,1280×720, hover, xác nhận, cuộn menu/trang và toàn trang đã capture.
- Không tràn ngang tại1280×720 trong lượt kiểm tra.
- Video hover/click/bụi sao và cuộn quay từ trình duyệt thật.
- Icon PNG được raster từ SVG local, không lấy ảnh/icon ngẫu nhiên từ Internet.
- JSON parse, đường dẫn assets, hashes và CRC ZIP được kiểm tra khi đóng gói.

## Chưa chạy

WinForms .NET10/Windows, DPI125/150%, Designer round-trip, input IME/Narrator, SQL thật, phân quyền, timeout/rollback/in lỗi, FPS/GDI handles. Không tick các ca Txx-Kxx/UX-Kxx từ kết quả ở đây.

Ảnh/video là bằng chứng browser cho thiết kế, không phải nghiệm thu app. Không tuyên bố mọi platform có pixel giống hệt; renderer font và native controls cần kiểm tra riêng.
