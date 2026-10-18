# Regression B01–B03 — 17/10/2026

Người dùng đã duyệt đợt debug và regression. Chỉ sửa WinForms; không sửa SQL,
DAL, BLL, Models, contract, query, paging hoặc event nghiệp vụ. Task vẫn Đang làm.

## Source và môi trường

- Windows, .NET 10, SQL Server localhost; DPI thật 120 (125%).
- DB fixture riêng có marker: `CoffeeManager_Test_G02_20261017_173711_c86ff044`.
- `source-freeze.csv` và `source-after.csv`: không có hash thay đổi trong lượt.
- Launcher hiển thị Form thật, pre-auth qua BLL; thao tác UI bằng sky. Không tự động nhập mật khẩu.
- Cửa sổ bị maximize trong một số thao tác; chỉ ảnh kèm geometry mới dùng xác nhận kích thước cố định.

## Thay đổi đã thực hiện

- B01: bật native dark mode trước khi tạo Form; OrbitTabControl chỉ tô chrome ngoài
  TabPages, giữ native selection/bàn phím. Không thay DateTimePicker nhập giờ.
- B02: giảm phần trống heading, chuyển hai nút phân trang xuống footer, giữ font,
  binding, paging và handler hiện có.
- B03: padding header riêng, WrapMode và chiều cao header AutoSize theo nội dung.

## Kết quả

| Ca | Expected / actual | Kết quả và bằng chứng |
|---|---|---|
| B01 tab | Không còn dải trắng, tab đổi được | PASS ở DPI125%; `main-1280-125-fixed.png`, `delivery-header-fixed.png`, `position-header-fixed.png` |
| B01 giờ | Ô giờ theo theme, giữ native input | FAIL về màu: vẫn trắng, hiển thị 06:00/14:00. `shift-native-hour-residual.png`. Không lưu ca thử |
| B02 1280×720 | Có thể đọc hàng DM001, toolbar và footer | PASS ở DPI125%; `main-1280-125-fixed.png`, `main-ADMIN-1280x720-geometry.json` |
| B02 phân trang | Sau ra trang2 rỗng, Trước trở lại trang1 có dòng | PASS; `pagination-next.png`, `pagination-previous.png`. Ảnh sau có cửa sổ maximize, không dùng suy ra kích thước1280 |
| B03 Ratio | Đọc đủ Chiết khấu (%) | PASS ở DPI125%; `delivery-header-fixed.png` |
| B03 Factor | Đọc đủ Hệ số tăng ca | PASS ở DPI125%; `position-header-fixed.png` |
| Unit regression AuthRules/CatalogRules | Không đổi kết quả quy tắc | 36/36 PASS; `unit.trx` |
| Build Debug | 0 lỗi/cảnh báo | PASS; `build-final-debug.txt` |
| Build Release | 0 lỗi/cảnh báo | PASS; build ra `release-app`, 0 warning/error |
| Whitespace | Không có lỗi diff | PASS; `git diff --check`, chỉ cảnh báo LF/CRLF |
| Source cố định | Hash trước/sau giống nhau | PASS; hai manifest CSV |
| DPI100/150, Designer, 1366 sau sửa, bảng G01 sau sửa | Cần thao tác app thật | NOT RUN/BLOCKED; không kế thừa thành PASS từ ảnh trước sửa |

484/484 G02 PASS là kết quả đợt acceptance trước, không chạy lại484ca trong lượt này.
36 unit trên nằm trong bộ484, không cộng thành520ca khác nhau.

## B01 còn lại và quyết định cần bổ sung

Đã thử native dark mode và opt-in theming trong CreateParams trên DateTimePicker;
ảnh thực tế vẫn trắng. Helper thử đã bỏ, trả về control native hiện có. Không dùng
thủ thuật recolor hoặc thay editor trong phạm vi giữ nguyên hành vi đã duyệt.

Đề xuất giữ ô giờ native trắng làm ngoại lệ để bảo toàn tương tác. Nếu bắt buộc
đồng màu, cần duyệt riêng editor HH:mm và kiểm lại nhập giờ, bàn phím, validation,
DPI, accessibility; không thay DB/BLL nhưng có thay đổi tương tác nhập liệu.

Tham khảo API chính thức: [WinForms .NET10 dark mode](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net100).
Khả năng đổi màu ô giờ ở đây là kết quả quan sát ứng dụng, không suy từ tài liệu API.

Các lỗi khởi chạy ban đầu do thứ tự initialize của fixture và build bị khóa file
đã được xử lý trước freeze (build sang thư mục riêng). `launch-error.txt` lưu lịch sử
fixture, không phải bug sản phẩm mới. Không commit/push.
