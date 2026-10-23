# Kiểm tra bản tham chiếu

- Đủ 9 HTML độc lập, không tải CSS/JS/font qua mạng.
- Chạy trên Chromium tại viewport 1366 × 768, deviceScaleFactor 1; cả 9 màn không tràn kích thước trang.
- Kiểm tra thêm chiều rộng 1024 px: không tràn ngang trang; bảng có vùng cuộn riêng.
- Không có lỗi JavaScript khi tải và thao tác được kiểm tra.
- Đã kiểm tra tìm “duong” ra “Đường”; lọc Lít; chuyển trang nguyên liệu; bộ lọc Chỉ đang bán; mở/đóng thông báo nghiệp vụ; điều hướng sidebar; chọn Bàn 02 và chuyển sang Danh sách.
- Đã kiểm tra Hạng thành viên không có SVG trong các ô dữ liệu.
- Đã xem ảnh render cả 9 màn để kiểm tra bố cục.

Giới hạn: chưa tích hợp WinForms, backend hay dữ liệu thật; chưa kiểm chứng bằng trình duyệt Windows có Segoe UI. Có thể có khác biệt raster hóa chữ giữa các hệ thống. Ảnh chuẩn gốc được giữ trong references/ để so sánh.
