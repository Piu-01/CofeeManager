# Orbit Café — 9 màn HTML

Mở một trong 9 file HTML bằng trình duyệt. Không cần npm, build, server hay mạng. Mỗi file chứa CSS, dữ liệu và JavaScript riêng. Sidebar và các tab liên quan dẫn tới file tương ứng trong cùng thư mục.

## Danh sách
1. `01-nguyen-lieu-ton-kho.html` — Nguyên liệu & tồn kho
2. `02-menu-cong-thuc.html` — Menu & công thức
3. `03-ban-so-do.html` — Bàn & khu vực
4. `04-khu-vuc.html` — Bàn & khu vực
5. `05-khach-hang.html` — Khách hàng
6. `06-khuyen-mai.html` — Khuyến mãi
7. `07-hang-thanh-vien.html` — Chính sách hạng
8. `08-diem-khach-hang.html` — Lịch sử điểm
9. `09-danh-muc-phuong-thuc.html` — Danh mục nền

## Cách dùng với agent WinForms
- Đọc HTML/CSS thật và design-tokens.json; ảnh trong references/ là mẫu gốc mới nhất để đối chiếu. Không dùng ảnh nhúng làm giao diện.
- Mẫu ưu tiên viewport 1366 × 768, trình duyệt zoom 100%; khi so sánh WinForms cần thống nhất kích thước client và DPI.
- Đây là reference web, không phải component C# WinForms. Font có thể khác đôi chút nếu hệ thống không có Segoe UI.
- Giữ nghiệp vụ hiện hữu; không thêm cột hay tự suy diễn tính năng. Bản Hạng thành viên không có icon cạnh tên hạng.
- Dùng một Theme/AppShell và control dùng chung. Hoàn thiện màn Nguyên liệu trước, chụp ảnh thực tế đối chiếu rồi mới nhân rộng.
- Bảng và lưới bàn cuộn bên trong. Header, công cụ, tab và phân trang ở ngoài vùng cuộn. Hàng không kéo giãn để lấp màn hình.
- Hover, press và reveal khi cuộn được minh họa; không có bụi sao trên 9 màn này. Bụi sao chỉ cho xác nhận thanh toán hoặc nút được chỉ định riêng.

## Tương tác có thể kiểm tra
Tìm kiếm không phân biệt dấu, lọc đơn vị/danh mục, Chỉ đang bán, chọn dòng hoặc bàn, phân trang nguyên liệu, chuyển lưới/danh sách bàn và điều hướng các màn đã có.

Các nút nghiệp vụ mở thông báo phạm vi bản tham chiếu, KHÔNG giả lập hoàn thành giao dịch. Các tab danh mục chưa có thiết kế hiển thị thông báo, không tự tạo màn. Khu vực của từng bàn chưa có mapping trong mẫu nên bộ lọc bàn chỉ có Tất cả khu vực. Chọn khách/Điều chỉnh điểm cần nối nghiệp vụ dự án; lịch sử giữ trạng thái rỗng như ảnh gốc. Checkbox Hỗ trợ thủ công là dữ liệu chỉ đọc của bảng, không tự sửa trực tiếp.

Không tự đưa dòng Bản xem thử vào sản phẩm thật. Các bản PNG gốc có thể cắt bớt dòng do vùng cuộn; toàn bộ dữ liệu mẫu có trong HTML.

## Ưu tiên khi có mâu thuẫn
Yêu cầu mới nhất của người dùng → quy tắc đã chốt → ảnh chuẩn và đặc tả HTML/CSS. Nếu source nghiệp vụ và mẫu không khớp, giữ nghiệp vụ, chỉ ra điểm khác biệt; không tự thêm hành vi.
